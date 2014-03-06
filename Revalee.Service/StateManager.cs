using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Revalee.Service
{
	// This class will store and persist the list of requested callbacks

	internal class StateManager : IDisposable
	{
		private readonly WaitingList<RevaleeTask> _AwaitingTasks = new WaitingList<RevaleeTask>(20);
		private readonly AgingList<Guid> _CancellationList = new AgingList<Guid>();
		private readonly object _SyncRoot = new object();
		private Type _PersistenceProviderType;
		private ITaskPersistenceProvider _PersistenceProvider = new NullTaskPersistenceProvider();

		public StateManager()
		{
		}

		public void Initialize()
		{
			lock (_SyncRoot)
			{
				if (_PersistenceProviderType == null)
				{
					_PersistenceProviderType = Supervisor.Configuration.TaskPersistenceProvider;
					_PersistenceProvider = (ITaskPersistenceProvider)Activator.CreateInstance(_PersistenceProviderType);
					_PersistenceProvider.Open(Supervisor.Configuration.TaskPersistenceConnectionString);

					if (_AwaitingTasks.Count == 0)
					{
						int recoveredTasks = RecoverTasks();

						if (recoveredTasks > 0)
						{
							Supervisor.LogEvent(string.Format("Recovered {0:#,##0} tasks from storage.", recoveredTasks), TraceEventType.Information);
						}
					}
				}
			}
		}

		public void Resume()
		{
		}

		public void Suspend()
		{
		}

		public void AddTask(RevaleeTask task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}

			lock (_SyncRoot)
			{
				_AwaitingTasks.Add(task, task.CallbackTime);
				_PersistenceProvider.AddTask(task);
			}

			Supervisor.Telemetry.IncrementAwaitingTasksValue();

			if (!Supervisor.IsPaused)
			{
				Supervisor.Time.Start();
			}
		}

		public RevaleeTask DoleTask()
		{
			lock (_SyncRoot)
			{
				while (_AwaitingTasks.ContainsOverdue)
				{
					RevaleeTask task = _AwaitingTasks.Dequeue();

					if (_CancellationList.Contains(task.CallbackId))
					{
						continue;
					}

					if (task.AttemptsRemaining == 0)
					{
						_PersistenceProvider.RemoveTask(task);
						Supervisor.Telemetry.DecrementAwaitingTasksValue();
						continue;
					}

					return task;
				}

				return null;
			}

		}

		public void CancelTask(RevaleeTask task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}

			RevaleeTask storedTask = RetrieveTask(task.CallbackId);

			if (storedTask != null)
			{
				if (storedTask.CallbackUrl.ToString().StartsWith(task.CallbackUrl.ToString(), StringComparison.OrdinalIgnoreCase))
				{
					lock (_SyncRoot)
					{
						_CancellationList.Add(storedTask.CallbackId, storedTask.CallbackTime);
						_PersistenceProvider.RemoveTask(task);
					}

					Supervisor.Telemetry.DecrementAwaitingTasksValue();
				}
			}
		}

		public DateTimeOffset? NextTaskTime
		{
			get
			{
				lock (_SyncRoot)
				{
					return _AwaitingTasks.PeekNextTime();
				}
			}
		}

		public void CompleteTask(RevaleeTask task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}

			lock (_SyncRoot)
			{
				_PersistenceProvider.RemoveTask(task);
				Supervisor.Telemetry.DecrementAwaitingTasksValue();
			}
		}

		public void UpdateTask(RevaleeTask task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}

			lock (_SyncRoot)
			{
				_PersistenceProvider.RemoveTask(task);
				_PersistenceProvider.AddTask(task);
			}
		}

		public void ReenlistTask(RevaleeTask task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}

			lock (_SyncRoot)
			{
				_AwaitingTasks.Add(task, task.CallbackTime);
			}
		}

		private int RecoverTasks()
		{
			int recoveredTasks = 0;

			foreach (RevaleeTask task in _PersistenceProvider.ListAllTasks())
			{
				_AwaitingTasks.Add(task, task.CallbackTime);
				recoveredTasks++;
			}

			if (recoveredTasks > 1000)
			{
				GC.Collect();
			}

			if (!Supervisor.IsPaused)
			{
				Supervisor.Time.Start();
			}

			Supervisor.Telemetry.SetAwaitingTasksValue(_AwaitingTasks.Count);

			return recoveredTasks;
		}

		private RevaleeTask RetrieveTask(Guid callbackId)
		{
			return _PersistenceProvider.GetTask(callbackId);
		}

		public int AwaitingTaskCount
		{
			get
			{
				return _AwaitingTasks.Count;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_PersistenceProvider != null)
				{
					_PersistenceProvider.Dispose();
				}
			}
		}
	}
}