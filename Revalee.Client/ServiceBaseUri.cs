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
using System.Runtime.Serialization;

namespace Revalee.Client
{
	/// <summary>Creates a <see cref="T:System.Uri"/> for use as the base Uri for the Revalee service.</summary>
	[Serializable]
	public class ServiceBaseUri : Uri
	{
		private const string _DefaultServiceScheme = "http";
		private const string _DefaultServiceHost = "localhost";
		private const int _DefaultHttpPortNumber = 46200;
		private const int _DefaultHttpsPortNumber = 46205;

		/// <summary>Initializes a new instance of the <see cref="T:Revalee.Client.ServiceBaseUri" /> class with the configured identifier.</summary>
		/// <exception cref="T:System.UriFormatException">The configured value is not valid as a service base Uri for the Revalee service.</exception>
		public ServiceBaseUri()
			: base(BuildConfiguredServiceBase(), UriKind.Absolute)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="T:Revalee.Client.ServiceBaseUri" /> class with the specified identifier.</summary>
		/// <param name="serviceHost">A DNS-style domain name, IP address, or full URL for the Revalee service.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="serviceHost" /> is null.</exception>
		/// <exception cref="T:System.UriFormatException"><paramref name="serviceHost" /> is not valid as a service base Uri for the Revalee service.</exception>
		public ServiceBaseUri(string serviceHost)
			: base(BuildSpecifiedServiceBase(serviceHost), UriKind.Absolute)
		{
		}

		protected ServiceBaseUri(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		private ServiceBaseUri(string uri, UriKind kind)
			: base(uri, kind)
		{
		}

		/// <summary>Creates a new instance of the <see cref="T:Revalee.Client.ServiceBaseUri" /> class with the specified identifier.</summary>
		/// <returns>A <see cref="T:System.Boolean" /> value that is true if the <see cref="T:Revalee.Client.ServiceBaseUri" /> was successfully created; otherwise, false.</returns>
		/// <param name="serviceHost">A DNS-style domain name, IP address, or full URL for the Revalee service.</param>
		/// <param name="uri">When this method returns, contains the constructed <see cref="T:Revalee.Client.ServiceBaseUri" />.</param>
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

		private static string BuildConfiguredServiceBase()
		{
			Uri configuredServiceBaseUri = RevaleeClientSettings.ServiceBaseUri;

			if (configuredServiceBaseUri == null)
			{
				return new UriBuilder(_DefaultServiceScheme, _DefaultServiceHost, _DefaultHttpPortNumber).ToString();
			}

			return configuredServiceBaseUri.ToString();
		}

		private static string BuildSpecifiedServiceBase(string serviceHost)
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
	}
}