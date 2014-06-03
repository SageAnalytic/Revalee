using Revalee.Service.EsePersistence;
using System;
using System.Collections.Generic;

namespace Revalee.Service
{
	/* Example provider strings:
	 *
	 * Microsoft.Isam.Esent
	 * System.Data.SqlClient
	 * System.Data.OleDb
	 * System.Data.Odbc
	 * System.Data.OracleClient
	 */

	internal static class TaskPersistenceProviderMapping
	{
		private static IDictionary<string, Type> _ProviderMappings = LoadProviderMappings();

		private static IDictionary<string, Type> LoadProviderMappings()
		{
			var mappings = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
			mappings.Add("Microsoft.Isam.Esent", typeof(EseTaskPersistenceProvider));
			return mappings;
		}

		public static Type MapProvider(string providerName)
		{
			if (string.IsNullOrWhiteSpace(providerName))
			{
				return typeof(NullTaskPersistenceProvider);
			}

			try
			{
				return _ProviderMappings[providerName.Trim()];
			}
			catch (KeyNotFoundException)
			{
				return null;
			}
		}
	}
}