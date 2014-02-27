#region License

/*
The MIT License (MIT)

Copyright (c) 2014 Sage Analytic Technologies, LLC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#endregion License

using System;

namespace Revalee.Client.Mvc
{
	internal class ServiceBaseUri : Uri
	{
		private const string _DefaultServiceScheme = "http";
		private const string _DefaultServiceHost = "localhost";
		private const int _DefaultHttpPortNumber = 46200;
		private const int _DefaultHttpsPortNumber = 46205;

		public ServiceBaseUri()
			: base(BuildServiceBase(), UriKind.Absolute)
		{
		}

		public ServiceBaseUri(string serviceHost)
			: base(BuildServiceBase(serviceHost), UriKind.Absolute)
		{
		}

		private ServiceBaseUri(string uri, UriKind kind)
			: base(uri, kind)
		{
		}

		private static string BuildServiceBase()
		{
			Uri configuredServiceBaseUri = RevaleeClientSettings.ServiceBaseUri;

			if (configuredServiceBaseUri == null)
			{
				return new UriBuilder(_DefaultServiceScheme, _DefaultServiceHost, _DefaultHttpPortNumber).ToString();
			}

			return configuredServiceBaseUri.ToString();
		}

		private static string BuildServiceBase(string serviceHost)
		{
			if (string.IsNullOrWhiteSpace(serviceHost))
			{
				throw new ArgumentNullException("serviceHost");
			}

			if (serviceHost.IndexOfAny(new char[] { ':', '/' }, 0) < 0)
			{
				if (Uri.CheckHostName(serviceHost) == UriHostNameType.Unknown)
				{
					throw new ArgumentException("Invalid host name specified for service host.", "serviceHost");
				}

				return (new UriBuilder(_DefaultServiceScheme, serviceHost, _DefaultHttpPortNumber)).ToString();
			}
			else
			{
				try
				{
					Uri proxyUri;

					if (serviceHost.IndexOf(Uri.SchemeDelimiter, StringComparison.OrdinalIgnoreCase) < 0)
					{
						proxyUri = new Uri(string.Concat(_DefaultServiceScheme, Uri.SchemeDelimiter, serviceHost), UriKind.Absolute);
					}
					else
					{
						proxyUri = new Uri(serviceHost, UriKind.Absolute);
					}

					if (proxyUri.HostNameType == UriHostNameType.Unknown)
					{
						throw new ArgumentException("Invalid host name specified for service host.", "serviceHost");
					}

					if (!proxyUri.IsAbsoluteUri || !(Uri.UriSchemeHttp.Equals(proxyUri.Scheme) || Uri.UriSchemeHttps.Equals(proxyUri.Scheme)))
					{
						throw new ArgumentException("Invalid scheme specified for service host.", "serviceHost");
					}

					if (proxyUri.IsDefaultPort)
					{
						if (Uri.UriSchemeHttp.Equals(proxyUri.Scheme, StringComparison.OrdinalIgnoreCase)
							&& serviceHost.LastIndexOf(":80", StringComparison.Ordinal) < (serviceHost.Length - 3))
						{
							// Incorrect default port
							return (new UriBuilder(proxyUri.Scheme, proxyUri.Host, _DefaultHttpPortNumber)).ToString();
						}
						else if (Uri.UriSchemeHttps.Equals(proxyUri.Scheme, StringComparison.OrdinalIgnoreCase)
							&& serviceHost.LastIndexOf(":443", StringComparison.Ordinal) < (serviceHost.Length - 4))
						{
							// Incorrect default port
							return (new UriBuilder(proxyUri.Scheme, proxyUri.Host, _DefaultHttpsPortNumber)).ToString();
						}
					}

					return proxyUri.ToString();
				}
				catch (UriFormatException ufex)
				{
					throw new ArgumentException("Invalid format specified for service host.", "serviceHost", ufex);
				}
			}
		}

		public static bool TryCreate(string serviceHost, out ServiceBaseUri uri)
		{
			if (!string.IsNullOrWhiteSpace(serviceHost))
			{
				if (serviceHost.IndexOfAny(new char[] { ':', '/' }, 0) < 0)
				{
					if (Uri.CheckHostName(serviceHost) != UriHostNameType.Unknown)
					{
						uri = new ServiceBaseUri(new UriBuilder(_DefaultServiceScheme, serviceHost, _DefaultHttpPortNumber).ToString(), UriKind.Absolute);
						return true;
					}
				}
				else
				{
					Uri proxyUri = null;

					if (serviceHost.IndexOf(Uri.SchemeDelimiter, StringComparison.OrdinalIgnoreCase) < 0)
					{
						Uri.TryCreate(string.Concat(_DefaultServiceScheme, Uri.SchemeDelimiter, serviceHost), UriKind.Absolute, out proxyUri);
					}
					else
					{
						Uri.TryCreate(serviceHost, UriKind.Absolute, out proxyUri);
					}

					if (proxyUri != null
						&& proxyUri.HostNameType != UriHostNameType.Unknown
						&& proxyUri.IsAbsoluteUri
						&& (Uri.UriSchemeHttp.Equals(proxyUri.Scheme) || Uri.UriSchemeHttps.Equals(proxyUri.Scheme)))
					{

						if (proxyUri.IsDefaultPort)
						{
							if (Uri.UriSchemeHttp.Equals(proxyUri.Scheme, StringComparison.OrdinalIgnoreCase)
								&& serviceHost.LastIndexOf(":80", StringComparison.Ordinal) < (serviceHost.Length - 3))
							{
								// Incorrect default port
								uri = new ServiceBaseUri(new UriBuilder(proxyUri.Scheme, proxyUri.Host, _DefaultHttpPortNumber).ToString(), UriKind.Absolute);
								return true;
							}
							else if (Uri.UriSchemeHttps.Equals(proxyUri.Scheme, StringComparison.OrdinalIgnoreCase)
								&& serviceHost.LastIndexOf(":443", StringComparison.Ordinal) < (serviceHost.Length - 4))
							{
								// Incorrect default port
								uri = new ServiceBaseUri(new UriBuilder(proxyUri.Scheme, proxyUri.Host, _DefaultHttpsPortNumber).ToString(), UriKind.Absolute);
								return true;
							}
						}
						else
						{
							uri = new ServiceBaseUri(proxyUri.ToString(), UriKind.Absolute);
							return true;
						}
					}
				}
			}

			uri = null;
			return false;
		}
	}
}