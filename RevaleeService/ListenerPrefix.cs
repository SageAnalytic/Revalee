using System;

namespace RevaleeService
{
	internal class ListenerPrefix
	{
		private const string _HostPlusReplacementTag = "____plus____";
		private const string _HostAsteriskReplacementTag = "____asterisk____";

		private readonly string _PrefixString;

		public string Scheme { get; private set; }

		public string Host { get; private set; }

		public int Port { get; private set; }

		public ListenerPrefix(string urlPrefix)
		{
			if (string.IsNullOrWhiteSpace(urlPrefix))
			{
				throw new ArgumentNullException("urlPrefix");
			}

			try
			{
				urlPrefix = EnsureEndingSlash(urlPrefix);

				var prefixUri = new Uri(EncodeHostWithinUrl(urlPrefix), UriKind.Absolute);

				if (!IsAbsoluteUrl(prefixUri))
				{
					throw new FormatException("Uri is not a valid listener prefix URL.");
				}

				_PrefixString = urlPrefix;
				this.Scheme = prefixUri.Scheme;
				this.Host = DecodeHost(prefixUri.Host);
				this.Port = prefixUri.Port;
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message, "urlPrefix");
			}
		}

		private ListenerPrefix(string prefixString, string scheme, string host, int port)
		{
			_PrefixString = prefixString;
			this.Scheme = scheme;
			this.Host = host;
			this.Port = port;
		}

		public static bool TryCreate(string urlPrefix, out ListenerPrefix prefix)
		{
			if (!string.IsNullOrWhiteSpace(urlPrefix))
			{
				Uri prefixUri = null;
				urlPrefix = EnsureEndingSlash(urlPrefix);

				if (Uri.TryCreate(EncodeHostWithinUrl(urlPrefix), UriKind.Absolute, out prefixUri))
				{
					if (!IsAbsoluteUrl(prefixUri))
					{
						prefix = null;
						return false;
					}

					prefix = new ListenerPrefix(urlPrefix, prefixUri.Scheme, DecodeHost(prefixUri.Host), prefixUri.Port);
					return true;
				}
			}

			prefix = null;
			return false;
		}

		public override string ToString()
		{
			return _PrefixString;
		}

		private static string DecodeHost(string host)
		{
			if (host.Equals(_HostPlusReplacementTag))
			{
				return "+";
			}
			else if (host.Equals(_HostAsteriskReplacementTag))
			{
				return "*";
			}
			else
			{
				return host;
			}
		}

		private static string EncodeHostWithinUrl(string url)
		{
			int schemeDelimiterIndex = url.IndexOf(Uri.SchemeDelimiter, 0);

			if (schemeDelimiterIndex > 0)
			{
				int hostIndex = schemeDelimiterIndex + Uri.SchemeDelimiter.Length;

				if (url.Length > (hostIndex + 2))
				{
					char firstHostCharacter = url[hostIndex];

					if (firstHostCharacter == '+')
					{
						return string.Concat(url.Substring(0, hostIndex), _HostPlusReplacementTag, url.Substring(hostIndex + 1));
					}
					else if (firstHostCharacter == '*')
					{
						return string.Concat(url.Substring(0, hostIndex), _HostAsteriskReplacementTag, url.Substring(hostIndex + 1));
					}
				}
			}

			return url;
		}

		private static string EnsureEndingSlash(string urlPrefix)
		{
			if (urlPrefix[urlPrefix.Length - 1] != '/')
			{
				urlPrefix += '/';
			}

			return urlPrefix;
		}

		private static bool IsAbsoluteUrl(Uri uri)
		{
			return (uri.IsAbsoluteUri && !uri.IsFile && !uri.IsUnc && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.UserInfo));
		}
	}
}