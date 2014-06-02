using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Xml;

namespace Revalee.Service
{
	internal class ConfigurationManager
	{
		private const string _DefaultListenerPrefix = "http://+:46200/";
		private const string _DefaultRetryIntervals = "PT1S,PT1M,PT1H";

		private const string _ServiceNameAppSettingsKey = "ServiceName";
		private const string _ListenerPrefixesAppSettingsKey = "ListenerPrefixes";
		private const string _RetryIntervalsAppSettingsKey = "RetryIntervals";
		private const string _TaskPersistenceConnectionStringsKey = "TaskPersistence";

		private string _ServiceName;
		private TaskPersistenceSettings _TaskPersistenceSettings;
		private UrlMatchDictionary<RevaleeUrlAuthorization> _AuthorizedTargets;
		private IList<ListenerPrefix> _ListenerPrefixes;
		private IList<TimeSpan> _RetryIntervals;

		public ConfigurationManager()
		{
			_ServiceName = LoadServiceNameSettings();
			_ListenerPrefixes = LoadListenerPrefixSettings();
			_RetryIntervals = LoadRetryIntervalSettings();
			_AuthorizedTargets = LoadUrlAuthorizationSettings(_ListenerPrefixes);
			_TaskPersistenceSettings = LoadTaskPersistenceSettings();
		}

		public string ServiceName
		{
			get
			{
				return _ServiceName;
			}
		}

		public TaskPersistenceSettings TaskPersistenceSettings
		{
			get
			{
				return _TaskPersistenceSettings;
			}
		}

		public IPartialMatchDictionary<Uri, RevaleeUrlAuthorization> AuthorizedTargets
		{
			get
			{
				return _AuthorizedTargets;
			}
		}

		public IList<ListenerPrefix> ListenerPrefixes
		{
			get
			{
				return _ListenerPrefixes;
			}
		}

		public IList<TimeSpan> RetryIntervals
		{
			get
			{
				return _RetryIntervals;
			}
		}

		public void ReloadAuthorizedTargets()
		{
			_AuthorizedTargets = LoadUrlAuthorizationSettings(_ListenerPrefixes);
		}

		private static string LoadServiceNameSettings()
		{
			string setting = System.Configuration.ConfigurationManager.AppSettings[_ServiceNameAppSettingsKey];

			if (string.IsNullOrWhiteSpace(setting))
			{
				return GetExecutingServiceName();
			}
			else
			{
				return setting.Trim();
			}
		}

		private static TaskPersistenceSettings LoadTaskPersistenceSettings()
		{
			ConnectionStringSettings connectionStringSettings = System.Configuration.ConfigurationManager.ConnectionStrings[_TaskPersistenceConnectionStringsKey];
			return new TaskPersistenceSettings(connectionStringSettings);
		}

		private static IList<ListenerPrefix> LoadListenerPrefixSettings()
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

		private static IList<TimeSpan> LoadRetryIntervalSettings()
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

		private static UrlMatchDictionary<RevaleeUrlAuthorization> LoadUrlAuthorizationSettings(IList<ListenerPrefix> listenerPrefixes)
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
						foreach (ListenerPrefix listenerPrefix in listenerPrefixes)
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

		private static string GetExecutingServiceName()
		{
			return Assembly.GetExecutingAssembly().GetName().Name;
		}
	}
}