using System.Configuration;

namespace Revalee.Service
{
	internal class UrlAuthorizationElementCollection : ConfigurationElementCollection
	{
		public UrlAuthorizationElement this[int index]
		{
			get
			{
				return (UrlAuthorizationElement)this.BaseGet(index);
			}
			set
			{
				if (this.BaseGet(index) != null)
				{
					this.BaseRemoveAt(index);
				}
				this.BaseAdd(index, value);
			}
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new UrlAuthorizationElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((UrlAuthorizationElement)element).UrlPrefix;
		}
	}
}