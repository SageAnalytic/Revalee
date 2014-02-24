using System.Configuration;

namespace Revalee.Service
{
	internal class SecuritySettingsConfigSection : ConfigurationSection
	{
		public static SecuritySettingsConfigSection GetConfig()
		{
			return (SecuritySettingsConfigSection)System.Configuration.ConfigurationManager.GetSection("securitySettings");
		}

		[ConfigurationProperty("urlAuthorizations", IsDefaultCollection = false), ConfigurationCollection(typeof(UrlAuthorizationElementCollection), AddItemName = "authorize")]
		public UrlAuthorizationElementCollection UrlAuthorizations
		{
			get { return (UrlAuthorizationElementCollection)this["urlAuthorizations"]; }
		}
	}
}