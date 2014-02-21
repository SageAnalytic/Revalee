using System;
using System.Collections.Generic;

namespace RevaleeService
{
	public interface ITaskPersistenceProvider : IDisposable
	{
		void Open(string connectionString);

		void Close();

		IEnumerable<RevaleeTask> ListAllTasks();

		IEnumerable<RevaleeTask> ListTasksDueBetween(DateTime startTime, DateTime endTime);

		RevaleeTask GetTask(Guid callbackId);

		void AddTask(RevaleeTask task);

		void RemoveTask(RevaleeTask task);
	}
}