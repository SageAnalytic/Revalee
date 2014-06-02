using System;
using System.Collections.Generic;
using System.Globalization;

namespace Revalee.Service
{
	internal class TaskExporter
	{
		private TaskExporter()
		{
		}

		public static void DumpToConsole()
		{
			var taskList = new SortedList<RevaleeTask, RevaleeTask>();
			TaskPersistenceSettings persistenceSettings;

			var config = new ConfigurationManager();
			persistenceSettings = config.TaskPersistenceSettings;

			// Load persisted tasks from the persistence provider
			ITaskPersistenceProvider persistenceProvider = persistenceSettings.CreateProvider();

			if (persistenceProvider is NullTaskPersistenceProvider)
			{
				Console.WriteLine("WARNING: Exporting tasks is not available, because there is no configured task persistence provider.");
				return;
			}

			try
			{
				persistenceProvider.Open(persistenceSettings.ConnectionString);

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
				Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:s}Z {1}", task.CallbackTime, task.CallbackUrl.OriginalString));
			}
		}
	}
}