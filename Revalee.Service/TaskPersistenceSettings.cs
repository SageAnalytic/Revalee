using System;
using System.Configuration;

namespace Revalee.Service
{
	internal class TaskPersistenceSettings
	{
		public TaskPersistenceSettings(ConnectionStringSettings connectionStringSettings)
		{
			if (connectionStringSettings == null)
			{
				this.ProviderType = typeof(NullTaskPersistenceProvider);
				this.ConnectionString = string.Empty;
			}
			else
			{
				string providerName = connectionStringSettings.ProviderName;
				Type providerType = TaskPersistenceProviderMapping.MapProvider(providerName);

				if (providerType == null)
				{
					throw new ConfigurationErrorsException(string.Format("The configured task persistence provider, '{0}', is not supported.", providerName),
						connectionStringSettings.ElementInformation.Source,
						connectionStringSettings.ElementInformation.LineNumber);
				}

				this.ProviderType = providerType;
				this.ConnectionString = connectionStringSettings.ConnectionString;
			}
		}

		public Type ProviderType
		{
			get;
			private set;
		}

		public string ConnectionString
		{
			get;
			private set;
		}

		public ITaskPersistenceProvider CreateProvider()
		{
			return (ITaskPersistenceProvider)Activator.CreateInstance(this.ProviderType);
		}
	}
}