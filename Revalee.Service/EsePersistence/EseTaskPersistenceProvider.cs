using Microsoft.Isam.Esent.Interop;
using Microsoft.Isam.Esent.Interop.Windows7;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Revalee.Service.EsePersistence
{
	internal class EseTaskPersistenceProvider : ITaskPersistenceProvider
	{
		private const string _DatabaseName = "RevaleeTasks";
		private const string _StorageEngineBaseName = "edb";
		private const int _ConnectionPoolSize = 10;

		private const string _TableNameCallbacks = "Callbacks";
		private const string _ColumnNameCallbackId = "CallbackId";
		private const string _ColumnNameCreatedTime = "CreatedTime";
		private const string _ColumnNameCallbackTime = "CallbackTime";
		private const string _ColumnNameCallbackUrl = "CallbackUrl";
		private const string _ColumnNameAttemptsRemaining = "AttemptsRemaining";
		private const string _ColumnNameAuthorizationCipher = "AuthorizationCipher";

		private Instance _EseInstance;
		private EseConnectionPool _ConnectionPool;
		private string _DatabasePath;

		private sealed class EseConnection : EsentResource
		{
			private readonly JET_INSTANCE _InstanceId;
			private readonly string _DatabasePath;
			private JET_SESID _SessionId = JET_SESID.Nil;
			private JET_DBID _DatabaseId = JET_DBID.Nil;
			private readonly Dictionary<string, IDictionary<string, JET_COLUMNID>> _TableSchema = new Dictionary<string, IDictionary<string, JET_COLUMNID>>();

			public EseConnection(JET_INSTANCE instanceId, string databasePath)
			{
				this._InstanceId = instanceId;
				this._DatabasePath = databasePath;
				Api.JetBeginSession(instanceId, out this._SessionId, null, null);
				base.ResourceWasAllocated();
				try
				{
					Api.JetAttachDatabase(this._SessionId, databasePath, AttachDatabaseGrbit.None);
					Api.JetOpenDatabase(this._SessionId, databasePath, null, out this._DatabaseId, OpenDatabaseGrbit.None);
				}
				catch (EsentErrorException eeex)
				{
					if (eeex.Error == JET_err.DatabaseNotFound || eeex.Error == JET_err.FileNotFound)
					{
						Api.JetCreateDatabase(this._SessionId, this._DatabasePath, null, out this._DatabaseId, CreateDatabaseGrbit.None);
					}
					else
					{
						throw;
					}
				}
			}

			public void Close()
			{
				if (this.HasResource)
				{
					this.ReleaseResource();
				}
			}

			public Table GetTable(string tableName)
			{
				return GetTable(tableName, OpenTableGrbit.None);
			}

			public Table GetTable(string tableName, OpenTableGrbit options)
			{
				base.CheckObjectIsNotDisposed();
				var table = new Table(this._SessionId, this._DatabaseId, tableName, options);

				lock (this._TableSchema)
				{
					if (!this._TableSchema.ContainsKey(tableName))
					{
						this._TableSchema.Add(tableName, Api.GetColumnDictionary(this._SessionId, table));
					}
				}

				return table;
			}

			public IDictionary<string, JET_COLUMNID> GetSchema(string tableName)
			{
				base.CheckObjectIsNotDisposed();
				IDictionary<string, JET_COLUMNID> columnDictionary = null;
				lock (this._TableSchema)
				{
					if (this._TableSchema.TryGetValue(tableName, out columnDictionary))
					{
						return columnDictionary;
					}

					using (var table = new Table(this._SessionId, this._DatabaseId, tableName, OpenTableGrbit.ReadOnly))
					{
						columnDictionary = Api.GetColumnDictionary(this._SessionId, table);
						this._TableSchema.Add(tableName, columnDictionary);
					}
				}
				return columnDictionary;
			}

			public JET_SESID Session
			{
				get
				{
					base.CheckObjectIsNotDisposed();
					return this._SessionId;
				}
			}

			public JET_DBID Database
			{
				get
				{
					base.CheckObjectIsNotDisposed();
					return this._DatabaseId;
				}
			}

			public static implicit operator JET_SESID(EseConnection connection)
			{
				return connection.Session;
			}

			protected override void ReleaseResource()
			{
				if (this._DatabaseId != JET_DBID.Nil)
				{
					// Detaching from the session is not necessary if the session will also be closed
					// To force a detach database command, use: Api.JetDetachDatabase(Session, this._DatabasePath);
					this._DatabaseId = JET_DBID.Nil;
				}

				if (this._SessionId != JET_SESID.Nil)
				{
					try
					{
						Api.JetEndSession(this._SessionId, EndSessionGrbit.None);
					}
					catch
					{ }
					this._SessionId = JET_SESID.Nil;
				}

				base.ResourceWasReleased();
			}
		}

		private sealed class EseConnectionPool : IDisposable
		{
			private readonly EseConnection[] _ConnectionPool;
			private readonly object _ConnectionLock = new object();
			private readonly JET_INSTANCE _InstanceId;
			private readonly string _DatabasePath;
			private readonly int _PoolSize;
			private int _HighWaterMark;

			public EseConnectionPool(JET_INSTANCE instanceId, string databasePath, int poolSize)
			{
				if (instanceId == JET_INSTANCE.Nil)
				{
					throw new ArgumentNullException("instanceId");
				}

				if (string.IsNullOrEmpty(databasePath))
				{
					throw new ArgumentNullException("databasePath");
				}

				if (poolSize <= 0)
				{
					throw new ArgumentOutOfRangeException("poolSize");
				}

				this._InstanceId = instanceId;
				this._DatabasePath = databasePath;
				this._PoolSize = poolSize;
				this._ConnectionPool = new EseConnection[poolSize];
				this._HighWaterMark = -1;
			}

			public EseConnection OpenConnection()
			{
				lock (this._ConnectionLock)
				{
					for (int connectionIndex = 0; connectionIndex <= this._HighWaterMark; connectionIndex++)
					{
						EseConnection connection = this._ConnectionPool[connectionIndex];

						if (connection != null)
						{
							this._ConnectionPool[connectionIndex] = null;
							return connection;
						}
					}
				}

				return new EseConnection(this._InstanceId, this._DatabasePath);
			}

			public void CloseConnection(EseConnection connection)
			{
				lock (this._ConnectionLock)
				{
					for (int connectionIndex = 0; connectionIndex < this._PoolSize; connectionIndex++)
					{
						if (this._ConnectionPool[connectionIndex] == null)
						{
							this._ConnectionPool[connectionIndex] = connection;
							if (connectionIndex > this._HighWaterMark)
							{
								this._HighWaterMark = connectionIndex;
							}
							return;
						}
					}
				}

				connection.Dispose();
			}

			public void Dispose()
			{
				for (int connectionIndex = 0; connectionIndex <= this._HighWaterMark; connectionIndex++)
				{
					EseConnection connection = this._ConnectionPool[connectionIndex];
					if (connection != null)
					{
						this._ConnectionPool[connectionIndex] = null;
						connection.Dispose();
					}
				}

				GC.SuppressFinalize(this);
			}
		}

		public void Open(string connectionString)
		{
			if (_EseInstance == null)
			{
				if (string.IsNullOrWhiteSpace(connectionString))
				{
					connectionString = ApplicationFolderHelper.ApplicationFolderName;
				}

				this._DatabasePath = Path.Combine(connectionString, Path.ChangeExtension(_DatabaseName, _StorageEngineBaseName));

				_EseInstance = new Instance(_DatabaseName);
				_EseInstance.Parameters.CreatePathIfNotExist = true;
				_EseInstance.Parameters.CircularLog = true;
				_EseInstance.Parameters.Recovery = true;
				_EseInstance.Parameters.BaseName = _StorageEngineBaseName;
				_EseInstance.Parameters.MaxSessions = _ConnectionPoolSize * 2;
				_EseInstance.Parameters.NoInformationEvent = true;

				if (!string.IsNullOrEmpty(connectionString))
				{
					_EseInstance.Parameters.SystemDirectory = connectionString;
					_EseInstance.Parameters.LogFileDirectory = connectionString;
					_EseInstance.Parameters.TempDirectory = connectionString;
					_EseInstance.Parameters.AlternateDatabaseRecoveryDirectory = connectionString;
				}

				InitGrbit grbit = default(InitGrbit);
				if (EsentVersion.SupportsWindows7Features)
				{
					grbit = Windows7Grbits.ReplayIgnoreLostLogs;
				}
				else
				{
					grbit = InitGrbit.None;
				}

				_EseInstance.Init(grbit);
				_ConnectionPool = new EseConnectionPool(_EseInstance, _DatabasePath, _ConnectionPoolSize);

				EseConnection connection = _ConnectionPool.OpenConnection();

				try
				{
					using (connection.GetTable(_TableNameCallbacks, OpenTableGrbit.ReadOnly))
					{
					}
				}
				catch (EsentErrorException eeex)
				{
					if (eeex.Error == JET_err.ObjectNotFound)
					{
						CreateTaskTable(connection);
					}
					else
					{
						throw;
					}
				}
				finally
				{
					_ConnectionPool.CloseConnection(connection);
				}
			}
		}

		public void Close()
		{
			this.Dispose(true);
		}

		public RevaleeTask GetTask(Guid callbackId)
		{
			if (_EseInstance == null)
			{
				throw new InvalidOperationException("Storage provider has not been opened.");
			}

			EseConnection connection = _ConnectionPool.OpenConnection();

			try
			{
				using (Table table = connection.GetTable(_TableNameCallbacks, OpenTableGrbit.Updatable))
				{
					IDictionary<string, JET_COLUMNID> columnIds = connection.GetSchema(_TableNameCallbacks);

					Api.JetSetCurrentIndex(connection, table, null);
					Api.MakeKey(connection, table, callbackId, MakeKeyGrbit.NewKey);
					if (Api.TrySeek(connection, table, SeekGrbit.SeekEQ))
					{
						Guid? storedCallbackId = Api.RetrieveColumnAsGuid(connection, table, columnIds[_ColumnNameCallbackId]);
						DateTime? createdTime = Api.RetrieveColumnAsDateTime(connection, table, columnIds[_ColumnNameCreatedTime]);
						DateTime? callbackTime = Api.RetrieveColumnAsDateTime(connection, table, columnIds[_ColumnNameCallbackTime]);
						string callbackUrl = Api.RetrieveColumnAsString(connection, table, columnIds[_ColumnNameCallbackUrl]);
						int? attemptsRemainingColumn = Api.RetrieveColumnAsInt32(connection, table, columnIds[_ColumnNameAttemptsRemaining]);
						string authorizationCipher = Api.RetrieveColumnAsString(connection, table, columnIds[_ColumnNameAuthorizationCipher]);

						Uri callbackUri = null;

						if (callbackTime.HasValue
							&& Uri.TryCreate(callbackUrl, UriKind.Absolute, out callbackUri)
							&& createdTime.HasValue
							&& storedCallbackId.HasValue
							&& attemptsRemainingColumn.HasValue)
						{
							RevaleeTask revivedTask = RevaleeTask.Revive(
								DateTime.SpecifyKind(callbackTime.Value, DateTimeKind.Utc),
								callbackUri,
								DateTime.SpecifyKind(createdTime.Value, DateTimeKind.Utc),
								storedCallbackId.Value,
								attemptsRemainingColumn.Value,
								string.IsNullOrEmpty(authorizationCipher) ? null : authorizationCipher);

							return revivedTask;
						}
					}
				}

				return null;
			}
			finally
			{
				_ConnectionPool.CloseConnection(connection);
			}
		}

		public void AddTask(RevaleeTask task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}

			if (_EseInstance == null)
			{
				throw new InvalidOperationException("Storage provider has not been opened.");
			}

			EseConnection connection = _ConnectionPool.OpenConnection();

			try
			{
				using (Table table = connection.GetTable(_TableNameCallbacks, OpenTableGrbit.Updatable))
				{
					IDictionary<string, JET_COLUMNID> columnIds = connection.GetSchema(_TableNameCallbacks);

					using (var transaction = new Transaction(connection))
					{
						using (var update = new Update(connection, table, JET_prep.Insert))
						{
							Api.SetColumn(connection, table, columnIds[_ColumnNameCallbackId], task.CallbackId);
							Api.SetColumn(connection, table, columnIds[_ColumnNameCreatedTime], task.CreatedTime);
							Api.SetColumn(connection, table, columnIds[_ColumnNameCallbackTime], task.CallbackTime);
							Api.SetColumn(connection, table, columnIds[_ColumnNameCallbackUrl], task.CallbackUrl.OriginalString, Encoding.Unicode);
							Api.SetColumn(connection, table, columnIds[_ColumnNameAttemptsRemaining], task.AttemptsRemaining);

							if (task.AuthorizationCipher != null)
							{
								Api.SetColumn(connection, table, columnIds[_ColumnNameAuthorizationCipher], task.AuthorizationCipher, Encoding.Unicode);
							}

							update.Save();
						}

						transaction.Commit(CommitTransactionGrbit.None);
					}
				}
			}
			finally
			{
				_ConnectionPool.CloseConnection(connection);
			}
		}

		public void RemoveTask(RevaleeTask task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}

			if (_EseInstance == null)
			{
				throw new InvalidOperationException("Storage provider has not been opened.");
			}

			EseConnection connection = _ConnectionPool.OpenConnection();

			try
			{
				using (Table table = connection.GetTable(_TableNameCallbacks, OpenTableGrbit.Updatable))
				{
					using (var transaction = new Transaction(connection))
					{
						Api.JetSetCurrentIndex(connection, table, null);
						Api.MakeKey(connection, table, task.CallbackId, MakeKeyGrbit.NewKey);
						if (Api.TrySeek(connection, table, SeekGrbit.SeekEQ))
						{
							Api.JetDelete(connection, table);
							transaction.Commit(CommitTransactionGrbit.None);
						}
					}
				}
			}
			finally
			{
				_ConnectionPool.CloseConnection(connection);
			}
		}

		public IEnumerable<RevaleeTask> ListAllTasks()
		{
			if (_EseInstance == null)
			{
				throw new InvalidOperationException("Storage provider has not been opened.");
			}

			var taskList = new List<RevaleeTask>();

			EseConnection connection = this._ConnectionPool.OpenConnection();

			try
			{
				using (Table table = connection.GetTable(_TableNameCallbacks, OpenTableGrbit.DenyWrite | OpenTableGrbit.Preread | OpenTableGrbit.ReadOnly | OpenTableGrbit.Sequential))
				{
					IDictionary<string, JET_COLUMNID> columnIds = connection.GetSchema(_TableNameCallbacks);

					if (Api.TryMoveFirst(connection, table))
					{
						JET_SESID jetSession = connection;
						JET_TABLEID jetTable = table;
						JET_COLUMNID jetColumnCallbackId = columnIds[_ColumnNameCallbackId];
						JET_COLUMNID jetColumnCreatedTime = columnIds[_ColumnNameCreatedTime];
						JET_COLUMNID jetColumnCallbackTime = columnIds[_ColumnNameCallbackTime];
						JET_COLUMNID jetColumnCallbackUrl = columnIds[_ColumnNameCallbackUrl];
						JET_COLUMNID jetColumnAttemptsRemaining = columnIds[_ColumnNameAttemptsRemaining];
						JET_COLUMNID jetColumnAuthorizationCipher = columnIds[_ColumnNameAuthorizationCipher];

						do
						{
							Guid? callbackId = Api.RetrieveColumnAsGuid(jetSession, jetTable, jetColumnCallbackId);
							DateTime? createdTime = Api.RetrieveColumnAsDateTime(jetSession, jetTable, jetColumnCreatedTime);
							DateTime? callbackTime = Api.RetrieveColumnAsDateTime(jetSession, jetTable, jetColumnCallbackTime);
							string callbackUrl = Api.RetrieveColumnAsString(jetSession, jetTable, jetColumnCallbackUrl);
							int? attemptsRemainingColumn = Api.RetrieveColumnAsInt32(jetSession, jetTable, jetColumnAttemptsRemaining);
							string authorizationCipher = Api.RetrieveColumnAsString(jetSession, jetTable, jetColumnAuthorizationCipher);

							Uri callbackUri = null;

							if (callbackTime.HasValue
								&& Uri.TryCreate(callbackUrl, UriKind.Absolute, out callbackUri)
								&& createdTime.HasValue
								&& callbackId.HasValue
								&& attemptsRemainingColumn.HasValue)
							{
								RevaleeTask revivedTask = RevaleeTask.Revive(
									DateTime.SpecifyKind(callbackTime.Value, DateTimeKind.Utc),
									callbackUri,
									DateTime.SpecifyKind(createdTime.Value, DateTimeKind.Utc),
									callbackId.Value,
									attemptsRemainingColumn.Value,
									string.IsNullOrEmpty(authorizationCipher) ? null : authorizationCipher);

								taskList.Add(revivedTask);
							}
						} while (Api.TryMoveNext(jetSession, jetTable));
					}
				}
			}
			finally
			{
				_ConnectionPool.CloseConnection(connection);
			}

			return taskList;
		}

		public IEnumerable<RevaleeTask> ListTasksDueBetween(DateTime startTime, DateTime endTime)
		{
			if (_EseInstance == null)
			{
				throw new InvalidOperationException("Storage provider has not been opened.");
			}

			DateTime rangeStartTime = NormalizeDateTime(startTime);
			DateTime rangeEndTime = NormalizeDateTime(endTime);

			// Inclusive Upper Limit does not work properly for the CLR DateTime type.
			// Add the smallest amount of time that the Esent engine will detect to include the ending range inclusively.
			rangeEndTime = rangeEndTime.AddMilliseconds(1.0);

			var taskList = new List<RevaleeTask>();

			EseConnection connection = this._ConnectionPool.OpenConnection();

			try
			{
				using (Table table = connection.GetTable(_TableNameCallbacks, OpenTableGrbit.DenyWrite | OpenTableGrbit.Preread | OpenTableGrbit.ReadOnly | OpenTableGrbit.Sequential))
				{
					IDictionary<string, JET_COLUMNID> columnIds = connection.GetSchema(_TableNameCallbacks);
					Api.JetSetCurrentIndex(connection, table, "due");
					Api.MakeKey(connection, table, rangeStartTime, MakeKeyGrbit.NewKey);

					if (Api.TrySeek(connection, table, SeekGrbit.SeekGE))
					{
						Api.MakeKey(connection, table, rangeEndTime, MakeKeyGrbit.NewKey);
						if (Api.TrySetIndexRange(connection, table, SetIndexRangeGrbit.RangeInclusive | SetIndexRangeGrbit.RangeUpperLimit))
						{
							JET_SESID jetSession = connection;
							JET_TABLEID jetTable = table;
							JET_COLUMNID jetColumnCallbackId = columnIds[_ColumnNameCallbackId];
							JET_COLUMNID jetColumnCreatedTime = columnIds[_ColumnNameCreatedTime];
							JET_COLUMNID jetColumnCallbackTime = columnIds[_ColumnNameCallbackTime];
							JET_COLUMNID jetColumnCallbackUrl = columnIds[_ColumnNameCallbackUrl];
							JET_COLUMNID jetColumnAttemptsRemaining = columnIds[_ColumnNameAttemptsRemaining];
							JET_COLUMNID jetColumnAuthorizationCipher = columnIds[_ColumnNameAuthorizationCipher];

							do
							{
								Guid? callbackId = Api.RetrieveColumnAsGuid(jetSession, jetTable, jetColumnCallbackId);
								DateTime? createdTime = Api.RetrieveColumnAsDateTime(jetSession, jetTable, jetColumnCreatedTime);
								DateTime? callbackTime = Api.RetrieveColumnAsDateTime(jetSession, jetTable, jetColumnCallbackTime);
								string callbackUrl = Api.RetrieveColumnAsString(jetSession, jetTable, jetColumnCallbackUrl);
								int? attemptsRemainingColumn = Api.RetrieveColumnAsInt32(jetSession, jetTable, jetColumnAttemptsRemaining);
								string authorizationCipher = Api.RetrieveColumnAsString(jetSession, jetTable, jetColumnAuthorizationCipher);

								Uri callbackUri = null;

								if (callbackTime.HasValue
									&& Uri.TryCreate(callbackUrl, UriKind.Absolute, out callbackUri)
									&& createdTime.HasValue
									&& callbackId.HasValue
									&& attemptsRemainingColumn.HasValue)
								{
									RevaleeTask revivedTask = RevaleeTask.Revive(
										DateTime.SpecifyKind(callbackTime.Value, DateTimeKind.Utc),
										callbackUri,
										DateTime.SpecifyKind(createdTime.Value, DateTimeKind.Utc),
										callbackId.Value,
										attemptsRemainingColumn.Value,
										string.IsNullOrEmpty(authorizationCipher) ? null : authorizationCipher);

									taskList.Add(revivedTask);
								}
							} while (Api.TryMoveNext(jetSession, jetTable));
						}
					}
				}
			}
			finally
			{
				_ConnectionPool.CloseConnection(connection);
			}

			return taskList;
		}

		private void CreateTaskTable(EseConnection connection)
		{
			using (var transaction = new Transaction(connection))
			{
				JET_TABLEID tableId;
				Api.JetCreateTable(connection, connection.Database, _TableNameCallbacks, 16, 0, out tableId);
				try
				{
					ColumndefGrbit notNullSetting = ColumndefGrbit.None;

					if (EsentVersion.SupportsVistaFeatures || EsentVersion.SupportsWindows7Features)
					{
						notNullSetting = ColumndefGrbit.ColumnNotNULL;
					}

					var guidColumnDef = new JET_COLUMNDEF()
					{
						coltyp = JET_coltyp.Binary,
						cbMax = 16,
						grbit = ColumndefGrbit.ColumnFixed | notNullSetting
					};

					var datetimeColumnDef = new JET_COLUMNDEF()
					{
						coltyp = JET_coltyp.DateTime,
						grbit = ColumndefGrbit.ColumnFixed | notNullSetting
					};

					var urlColumnDef = new JET_COLUMNDEF()
					{
						coltyp = JET_coltyp.LongText,
						cbMax = 4096,
						cp = JET_CP.Unicode,
						grbit = notNullSetting
					};

					var integerColumnDef = new JET_COLUMNDEF()
					{
						coltyp = JET_coltyp.Long,
						grbit = notNullSetting
					};

					var cipherColumnDef = new JET_COLUMNDEF()
					{
						coltyp = JET_coltyp.Text,
						cbMax = 255,
						cp = JET_CP.Unicode,
						grbit = ColumndefGrbit.None
					};

					JET_COLUMNID columnId;

					Api.JetAddColumn(connection, tableId, _ColumnNameCallbackId, guidColumnDef, null, 0, out columnId);
					Api.JetAddColumn(connection, tableId, _ColumnNameCreatedTime, datetimeColumnDef, null, 0, out columnId);
					Api.JetAddColumn(connection, tableId, _ColumnNameCallbackTime, datetimeColumnDef, null, 0, out columnId);
					Api.JetAddColumn(connection, tableId, _ColumnNameCallbackUrl, urlColumnDef, null, 0, out columnId);
					Api.JetAddColumn(connection, tableId, _ColumnNameAttemptsRemaining, integerColumnDef, null, 0, out columnId);
					Api.JetAddColumn(connection, tableId, _ColumnNameAuthorizationCipher, cipherColumnDef, null, 0, out columnId);

					string primaryIndexDef = string.Format(CultureInfo.InvariantCulture, "+{1}{0}{0}", Convert.ToChar(0), _ColumnNameCallbackId);
					Api.JetCreateIndex(connection, tableId, "primary", CreateIndexGrbit.IndexPrimary, primaryIndexDef, primaryIndexDef.Length, 80);

					string alternateIndexDef = string.Format(CultureInfo.InvariantCulture, "+{1}{0}+{2}{0}{0}", Convert.ToChar(0), _ColumnNameCallbackTime, _ColumnNameCreatedTime);
					Api.JetCreateIndex(connection, tableId, "due", CreateIndexGrbit.IndexDisallowNull, alternateIndexDef, alternateIndexDef.Length, 80);
				}
				finally
				{
					Api.JetCloseTable(connection, tableId);
				}

				transaction.Commit(CommitTransactionGrbit.None);
			}
		}

		private static DateTime NormalizeDateTime(DateTime time)
		{
			if (time.Kind == DateTimeKind.Local)
			{
				return time.ToUniversalTime();
			}
			else if (time.Kind == DateTimeKind.Utc)
			{
				return time;
			}
			else
			{
				return DateTime.SpecifyKind(time, DateTimeKind.Utc);
			}
		}

		~EseTaskPersistenceProvider()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			// Re-opening a disposed provider will cause an ObjectDisposedException, use Close() to re-open a provider.
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			Instance workingInstance = Interlocked.Exchange(ref _EseInstance, null);

			if (workingInstance != null)
			{
				if (_ConnectionPool != null)
				{
					_ConnectionPool.Dispose();
				}

				if (!workingInstance.IsClosed)
				{
					workingInstance.Dispose();
				}
			}
		}
	}
}