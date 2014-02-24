using System;
using System.Collections.Generic;

namespace Revalee.Service
{
	internal class NullTaskPersistenceProvider : ITaskPersistenceProvider
	{
		public void Open(string connectionString)
		{
			return;
		}

		public void Close()
		{
			return;
		}

		public RevaleeTask GetTask(Guid callbackId)
		{
			return null;
		}

		public void AddTask(RevaleeTask Task)
		{
			return;
		}

		public void RemoveTask(RevaleeTask Task)
		{
			return;
		}

		public IEnumerable<RevaleeTask> ListAllTasks()
		{
			return new RevaleeTask[] { };
		}

		public IEnumerable<RevaleeTask> ListTasksDueBetween(DateTime startTime, DateTime endTime)
		{
			return new RevaleeTask[] { };
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}
}