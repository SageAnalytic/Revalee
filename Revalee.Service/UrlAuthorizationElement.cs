using System;
using System.Configuration;

namespace Revalee.Service
{
	internal class UrlAuthorizationElement : ConfigurationElement
	{
		[ConfigurationProperty("urlPrefix", IsRequired = true)]
		public string UrlPrefix
		{
			get { return Convert.ToString(this["urlPrefix"]); }
		}

		[ConfigurationProperty("fromAddresses", IsRequired = false)]
		public string FromAddresses
		{
			get { return Convert.ToString(this["fromAddresses"]); }
		}

		[ConfigurationProperty("retries", DefaultValue = "0", IsRequired = false)]
		public string Retries
		{
			get { return Convert.ToString(this["retries"]); }
		}
	}
}