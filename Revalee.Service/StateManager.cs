using System;
using System.Diagnostics;

namespace Revalee.Service
{
	// This class will store and persist the list of requested callbacks

	internal class StateManager : IDisposable
	{
		private readonly ScheduledDictionary<Guid, RevaleeTask> _AwaitingTaskCollection = new ScheduledDictionary<Guid, RevaleeTask>();
		private readonly object _SyncRoot = new object();
		private TaskPersistenceSettings _TaskPersistenceSettings = null;
		private ITaskPersistenceProvider _PersistenceProvider = new NullTaskPersistenceProvider();

		public StateManager()
		{
		}

		public void Initialize()
		{
			lock (_SyncRoot)
			{
				if (_TaskPersistenceSettings == null)
				{
					_TaskPersistenceSettings = Supervisor.Configuration.TaskPersistenceSettings;
					_PersistenceProvider = _TaskPersistenceSettings.CreateProvider();
					_PersistenceProvider.Open(_TaskPersistenceSettings.ConnectionString);

					_AwaitingTaskCollection.Clear();

					if (_PersistenceProvider is NullTaskPersistenceProvider)
					{
						Supervisor.LogEvent("No task persistence provider has been configured. Awaiting tasks will be lost when the service is shut down.", TraceEventType.Information);
					}
					else
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

			_AwaitingTaskCollection.AddOrReplace(task.CallbackId, task, task.CallbackTime);

			lock (_SyncRoot)
			{
				_PersistenceProvider.AddTask(task);
			}

			Supervisor.Telemetry.IncrementAwaitingTasksValue();
			ResetTaskAlarm();
		}

		public RevaleeTask DoleTask()
		{
			RevaleeTask task;

			while (_AwaitingTaskCollection.TryDequeue(out task))
			{
				if (task.AttemptsRemaining == 0)
				{
					lock (_SyncRoot)
					{
						_PersistenceProvider.RemoveTask(task);
					}

					Supervisor.Telemetry.DecrementAwaitingTasksValue();
					continue;
				}

				return task;
			}

			return null;
		}

		public void CancelTask(RevaleeTask task)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}

			RevaleeTask awaitingTask;

			if (_AwaitingTaskCollection.TryGetValue(task.CallbackId, out awaitingTask))
			{
				if (awaitingTask.CallbackUrl.ToString().StartsWith(task.CallbackUrl.ToString(), StringComparison.OrdinalIgnoreCase))
				{
					RevaleeTask removedTask;
					bool wasTaskRemoved = _AwaitingTaskCollection.TryRemove(awaitingTask.CallbackId, out removedTask);

					lock (_SyncRoot)
					{
						_PersistenceProvider.RemoveTask(awaitingTask);
					}

					if (wasTaskRemoved)
					{
						Supervisor.Telemetry.DecrementAwaitingTasksValue();
					}
				}
			}
			else
			{
				RevaleeTask storedTask = RetrieveTask(task.CallbackId);

				if (storedTask != null)
				{
					if (storedTask.CallbackUrl.ToString().StartsWith(task.CallbackUrl.ToString(), StringComparison.OrdinalIgnoreCase))
					{
						lock (_SyncRoot)
						{
							_PersistenceProvider.RemoveTask(storedTask);
						}
					}
				}
			}
		}

		public DateTime? NextTaskTime
		{
			get
			{
				DateTime nextDue;

				if (_AwaitingTaskCollection.TryPeekNextDue(out nextDue))
				{
					return nextDue;
				}

				return null;
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

			_AwaitingTaskCollection.AddOrReplace(task.CallbackId, task, task.CallbackTime);
			ResetTaskAlarm();
		}

		public void ReenlistTask(RevaleeTask task, DateTime due)
		{
			if (task == null)
			{
				throw new ArgumentNullException("task");
			}

			if (due.Kind != DateTimeKind.Utc)
			{
				throw new ArgumentException("DateTime argument not provided in UTC.", "due");
			}

			_AwaitingTaskCollection.AddOrReplace(task.CallbackId, task, due);
			ResetTaskAlarm();
		}

		private int RecoverTasks()
		{
			int recoveredTasks = 0;

			foreach (RevaleeTask task in _PersistenceProvider.ListAllTasks())
			{
				_AwaitingTaskCollection.AddOrReplace(task.CallbackId, task, task.CallbackTime);
				recoveredTasks++;
			}

			if (recoveredTasks > 1000)
			{
				GC.Collect();
			}

			Supervisor.Telemetry.SetAwaitingTasksValue(_AwaitingTaskCollection.Count);
			ResetTaskAlarm();
			return recoveredTasks;
		}

		private void ResetTaskAlarm()
		{
			if (!Supervisor.IsPaused)
			{
				Supervisor.Time.Start();
			}
		}

		private RevaleeTask RetrieveTask(Guid callbackId)
		{
			return _PersistenceProvider.GetTask(callbackId);
		}

		public int AwaitingTaskCount
		{
			get
			{
				return _AwaitingTaskCollection.Count;
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