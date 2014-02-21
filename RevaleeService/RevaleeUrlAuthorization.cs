using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace RevaleeService
{
	public class RevaleeUrlAuthorization
	{
		private Uri _UrlPrefix;
		private List<IPNetwork> _AuthorizedAddresses = new List<IPNetwork>();
		private int _RetryCount;

		public RevaleeUrlAuthorization(string urlPrefix, string fromAddresses, string retryCount)
		{
			if (string.IsNullOrWhiteSpace(urlPrefix))
			{
				throw new ArgumentNullException("urlPrefix");
			}

			_UrlPrefix = new Uri(urlPrefix, UriKind.Absolute);

			LoadFromAddresses(fromAddresses);

			if (!string.IsNullOrEmpty(retryCount))
			{
				if (!int.TryParse(retryCount, out _RetryCount))
				{
					_RetryCount = 0;
				}

				if (_RetryCount < 0 || _RetryCount > 100)
				{
					throw new ArgumentOutOfRangeException("RetryCount");
				}
			}
		}

		private void LoadFromAddresses(string value)
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				string[] addressList = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (addressList.Length > 0)
				{
					foreach (string address in addressList)
					{
						string trimmedAddress = address.Trim();
						if (trimmedAddress.Length > 0)
						{
							IPNetwork ipNetwork = IPNetwork.Parse(trimmedAddress);
							if (ipNetwork != null)
							{
								_AuthorizedAddresses.Add(ipNetwork);
							}
						}
					}
				}
			}
		}

		public bool IsAuthorizedRequestSource(IPAddress address)
		{
			// Zero authorization entries implies that no address restrictions are required
			if (_AuthorizedAddresses.Count == 0)
			{
				return true;
			}

			foreach (IPNetwork authorizedNetwork in _AuthorizedAddresses)
			{
				if (authorizedNetwork.IsAddressInNetwork(address))
				{
					return true;
				}
			}

			return false;
		}

		public Uri UrlPrefix
		{
			get { return _UrlPrefix; }
		}

		public int RetryCount
		{
			get { return _RetryCount; }
		}

		public static void ObfuscateExecutionTime()
		{
			byte[] milliseconds = new byte[2];

			using (var rng = new RNGCryptoServiceProvider())
			{
				rng.GetBytes(milliseconds);
			}

			Thread.Sleep(Math.Abs(milliseconds[0] + milliseconds[1]));
		}
	}
}