using Revalee.Service.EsePersistence;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace Revalee.Service
{
	internal class ConfigurationManager : IDisposable
	{
		private const string _DefaultListenerPrefix = "http://+:46200/";

		private UrlMatchDictionary<RevaleeUrlAuthorization> _AuthorizedTargets = new UrlMatchDictionary<RevaleeUrlAuthorization>();
		private ListenerPrefix[] _ListenerPrefixes;

		public ConfigurationManager()
		{
			LoadConfigFile();
		}

		public IPartialMatchDictionary<Uri, RevaleeUrlAuthorization> AuthorizedTargets
		{
			get { return _AuthorizedTargets; }
		}

		public ListenerPrefix[] ListenerPrefixes
		{
			get { return _ListenerPrefixes; }
		}

		public Type TaskPersistenceProvider
		{
			get
			{
				ConnectionStringSettings connectionStringSettings = System.Configuration.ConfigurationManager.ConnectionStrings["TaskPersistence"];
				if (connectionStringSettings != null)
				{
					if (connectionStringSettings.ProviderName.Equals("Microsoft.Isam.Esent", StringComparison.OrdinalIgnoreCase))
					{
						return typeof(EseTaskPersistenceProvider);
					}
				}

				return typeof(NullTaskPersistenceProvider);
			}
		}

		public string TaskPersistenceConnectionString
		{
			get
			{
				ConnectionStringSettings connectionStringSettings = System.Configuration.ConfigurationManager.ConnectionStrings["TaskPersistence"];
				if (connectionStringSettings != null)
				{
					return connectionStringSettings.ConnectionString;
				}

				return string.Empty;
			}
		}

		private void LoadConfigFile()
		{
			LoadListenerPrefixesSetting();
			LoadUrlAuthorizations();
		}

		private void LoadListenerPrefixesSetting()
		{
			string setting = System.Configuration.ConfigurationManager.AppSettings["ListenerPrefixes"];
			
			if (!string.IsNullOrEmpty(setting))
			{
				string[] urls = setting.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

				if (urls.Length > 0)
				{
					var acceptedPrefixes = new List<ListenerPrefix>();
					foreach (string url in urls)
					{
						ListenerPrefix prefix = null;

						if (ListenerPrefix.TryCreate(url.Trim(), out prefix))
						{
							if (prefix.Scheme.Equals(Uri.UriSchemeHttp) || prefix.Scheme.Equals(Uri.UriSchemeHttps))
							{
								acceptedPrefixes.Add(prefix);
							}
						}
					}

					if (acceptedPrefixes.Count > 0)
					{
						_ListenerPrefixes = acceptedPrefixes.ToArray();
						return;
					}
				} 
			}

			_ListenerPrefixes = new ListenerPrefix[] { new ListenerPrefix(_DefaultListenerPrefix) };
		}

		private void LoadUrlAuthorizations()
		{
			SecuritySettingsConfigSection section = SecuritySettingsConfigSection.GetConfig();

			if (section != null)
			{
				foreach (UrlAuthorizationElement authorizationElement in section.UrlAuthorizations)
				{
					var authorization = new RevaleeUrlAuthorization(authorizationElement.UrlPrefix,
						authorizationElement.FromAddresses,
						authorizationElement.Retries);

					if (authorization.UrlPrefix.IsLoopback)
					{
						foreach (ListenerPrefix listenerPrefix in _ListenerPrefixes)
						{
							if (authorization.UrlPrefix.Port == listenerPrefix.Port)
							{
								throw new UriFormatException(string.Format("Cannot authorize callbacks to {0} since that port is used by this service.", authorization.UrlPrefix));
							}
						}
					}

					_AuthorizedTargets.Add(authorization.UrlPrefix, authorization);
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
		}
	}
}