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
			ITaskPersistenceProvider persistenceProvider;

			using (var config = new ConfigurationManager())
			{
				// Load persisted tasks from the persistence provider
				persistenceProvider = (ITaskPersistenceProvider)Activator.CreateInstance(config.TaskPersistenceProvider);
			}

			if (persistenceProvider == null)
			{
				Console.WriteLine("ERROR: Cannot load the configured persistence provider.");
			}

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
				Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:s}Z {1}", task.CallbackTime, task.CallbackUrl.OriginalString));
			}
		}
	}
}