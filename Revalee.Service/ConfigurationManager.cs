using System;
using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace Revalee.Service
{
	internal class ConfigurationManager : IDisposable
	{
		private const string _DefaultListenerPrefix = "http://+:46200/";
		private const string _DefaultRetryIntervals = "PT1S,PT1M,PT1H";
		private const string _ListenerPrefixesAppSettingsKey = "ListenerPrefixes";
		private const string _RetryIntervalsAppSettingsKey = "RetryIntervals";
		private const string _TaskPersistenceConnectionStringsKey = "TaskPersistence";

		private TaskPersistenceSettings _TaskPersistenceSettings;
		private UrlMatchDictionary<RevaleeUrlAuthorization> _AuthorizedTargets;
		private IList<ListenerPrefix> _ListenerPrefixes;
		private IList<TimeSpan> _RetryIntervals;

		public ConfigurationManager()
		{
		}

		public TaskPersistenceSettings TaskPersistenceSettings
		{
			get
			{
				if (_TaskPersistenceSettings == null)
				{
					TaskPersistenceSettings persistenceSettings = LoadTaskPersistenceSettings();
					_TaskPersistenceSettings = persistenceSettings;
					return persistenceSettings;
				}

				return _TaskPersistenceSettings;
			}
		}

		public IPartialMatchDictionary<Uri, RevaleeUrlAuthorization> AuthorizedTargets
		{
			get
			{
				if (_AuthorizedTargets == null)
				{
					UrlMatchDictionary<RevaleeUrlAuthorization> authorizedTargets = LoadUrlAuthorizationSettings();
					_AuthorizedTargets = authorizedTargets;
					return authorizedTargets;
				}

				return _AuthorizedTargets;
			}
		}

		public IList<ListenerPrefix> ListenerPrefixes
		{
			get
			{
				if (_ListenerPrefixes == null)
				{
					IList<ListenerPrefix> listenerPrefixes = LoadListenerPrefixSettings();
					_ListenerPrefixes = listenerPrefixes;
					return listenerPrefixes;
				}

				return _ListenerPrefixes;
			}
		}

		public IList<TimeSpan> RetryIntervals
		{
			get
			{
				if (_RetryIntervals == null)
				{
					IList<TimeSpan> retryIntervals = LoadRetryIntervalSettings();
					_RetryIntervals = retryIntervals;
					return retryIntervals;
				}

				return _RetryIntervals;
			}
		}

		public void Initialize()
		{
			_ListenerPrefixes = LoadListenerPrefixSettings();
			_RetryIntervals = LoadRetryIntervalSettings();
			_AuthorizedTargets = LoadUrlAuthorizationSettings();
			_TaskPersistenceSettings = LoadTaskPersistenceSettings();
		}

		public void ReloadAuthorizedTargets()
		{
			_AuthorizedTargets = LoadUrlAuthorizationSettings();
		}

		private TaskPersistenceSettings LoadTaskPersistenceSettings()
		{
			ConnectionStringSettings connectionStringSettings = System.Configuration.ConfigurationManager.ConnectionStrings[_TaskPersistenceConnectionStringsKey];
			return new TaskPersistenceSettings(connectionStringSettings);
		}

		private IList<ListenerPrefix> LoadListenerPrefixSettings()
		{
			string setting = System.Configuration.ConfigurationManager.AppSettings[_ListenerPrefixesAppSettingsKey];

			if (string.IsNullOrWhiteSpace(setting))
			{
				setting = _DefaultListenerPrefix;
			}

			string[] urls = setting.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

			if (urls.Length == 0)
			{
				ElementInformation listenerPrefixesElement = GetAppSettingElementInformation(_ListenerPrefixesAppSettingsKey);
				throw new ConfigurationErrorsException("The specified listener prefix setting contains invalid data.", listenerPrefixesElement.Source, listenerPrefixesElement.LineNumber);
			}

			var listenerPrefixesList = new List<ListenerPrefix>();

			foreach (string url in urls)
			{
				ListenerPrefix prefix = null;

				if (ListenerPrefix.TryCreate(url.Trim(), out prefix))
				{
					if (!prefix.Scheme.Equals(Uri.UriSchemeHttp) && !prefix.Scheme.Equals(Uri.UriSchemeHttps))
					{
						ElementInformation listenerPrefixesElement = GetAppSettingElementInformation(_ListenerPrefixesAppSettingsKey);
						throw new ConfigurationErrorsException(string.Format("The specified listener prefix, '{0}', must start with either {1} or {2}.", url.Trim(), Uri.UriSchemeHttp, Uri.UriSchemeHttps),
							listenerPrefixesElement.Source,
							listenerPrefixesElement.LineNumber);
					}

					listenerPrefixesList.Add(prefix);
				}
				else
				{
					ElementInformation listenerPrefixesElement = GetAppSettingElementInformation(_ListenerPrefixesAppSettingsKey);
					throw new ConfigurationErrorsException(string.Format("The specified listener prefix, '{0}', is not valid.", url.Trim()),
						listenerPrefixesElement.Source,
						listenerPrefixesElement.LineNumber);
				}
			}

			return listenerPrefixesList;
		}

		private IList<TimeSpan> LoadRetryIntervalSettings()
		{
			string setting = System.Configuration.ConfigurationManager.AppSettings[_RetryIntervalsAppSettingsKey];

			if (string.IsNullOrWhiteSpace(setting))
			{
				setting = _DefaultRetryIntervals;
			}

			string[] intervals = setting.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

			if (intervals.Length == 0)
			{
				ElementInformation retryIntervalsElement = GetAppSettingElementInformation(_RetryIntervalsAppSettingsKey);
				throw new ConfigurationErrorsException("The specified retry intervals setting contains invalid data.", retryIntervalsElement.Source, retryIntervalsElement.LineNumber);
			}

			var retryIntervalsList = new List<TimeSpan>();

			foreach (string interval in intervals)
			{
				try
				{
					TimeSpan retryInterval = XmlConvert.ToTimeSpan(interval.Trim());

					if (retryInterval < TimeSpan.Zero)
					{
						ElementInformation retryIntervalsElement = GetAppSettingElementInformation(_RetryIntervalsAppSettingsKey);
						throw new ConfigurationErrorsException("A specified retry interval cannot be negative.", retryIntervalsElement.Source, retryIntervalsElement.LineNumber);
					}

					retryIntervalsList.Add(retryInterval);
				}
				catch (FormatException fex)
				{
					ElementInformation retryIntervalsElement = GetAppSettingElementInformation(_RetryIntervalsAppSettingsKey);
					throw new ConfigurationErrorsException(string.Format("The specified retry interval, '{0}', is not a valid XML duration.", interval.Trim()),
						fex,
						retryIntervalsElement.Source,
						retryIntervalsElement.LineNumber);
				}
			}

			return retryIntervalsList;
		}

		private UrlMatchDictionary<RevaleeUrlAuthorization> LoadUrlAuthorizationSettings()
		{
			var authorizedTargets = new UrlMatchDictionary<RevaleeUrlAuthorization>();

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
								throw new ConfigurationErrorsException(string.Format("Cannot authorize callbacks to {0} since that port is used by this service.", authorization.UrlPrefix),
									authorizationElement.ElementInformation.Source,
									authorizationElement.ElementInformation.LineNumber);
							}
						}
					}

					authorizedTargets.Add(authorization.UrlPrefix, authorization);
				}
			}

			return authorizedTargets;
		}

		private static ElementInformation GetAppSettingElementInformation(string key)
		{
			System.Configuration.Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			AppSettingsSection section = (System.Configuration.AppSettingsSection)config.GetSection("appSettings");
			return section.Settings[key].ElementInformation;
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