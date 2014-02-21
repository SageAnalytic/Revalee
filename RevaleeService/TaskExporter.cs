using System;
using System.Collections.Generic;

namespace RevaleeService
{
	internal class TaskExporter
	{
		private TaskExporter()
		{
		}

		public static void DumpToConsole()
		{
			var config = new ConfigurationManager();
			var taskList = new SortedList<RevaleeTask, RevaleeTask>();

			// Load persisted tasks from the persistence provider
			ITaskPersistenceProvider persistenceProvider = (ITaskPersistenceProvider)Activator.CreateInstance(config.TaskPersistenceProvider);
			try
			{
				persistenceProvider.Open(Supervisor.Configuration.TaskPersistenceConnectionString);

				foreach (RevaleeTask task in persistenceProvider.ListAllTasks())
				{
					taskList.Add(task, task);
				}
			}
			finally
			{
				persistenceProvider.Close();
			}

			// Write to the console
			foreach (RevaleeTask task in taskList.Values)
			{
				Console.WriteLine(string.Format("{0:s}Z {1}", task.CallbackTime, task.CallbackUrl.OriginalString));
			}
		}
	}
}