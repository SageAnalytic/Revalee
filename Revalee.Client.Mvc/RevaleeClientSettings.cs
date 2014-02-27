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
using System.Configuration;
using System.Web;
using RevaleeClientLibrary = Revalee.Client.Mvc;

namespace Revalee.Client.Mvc
{
	/// <summary>Represents the configured values used to make callback requests.</summary>
	public static class RevaleeClientSettings
	{
		private const string _AuthorizationKeyAppSettingsKey = "RevaleeAuthorizationKey";
		private const string _RequestTimeoutAppSettingsKey = "RevaleeRequestTimeout";
		private const string _ServiceBaseUriAppSettingsKey = "RevaleeServiceBaseUri";

		internal static string AuthorizationKey
		{
			get
			{
				HttpContext context = HttpContext.Current;

				if (context != null)
				{
					string overrideValue = context.Items[_AuthorizationKeyAppSettingsKey] as string;

					if (!string.IsNullOrEmpty(overrideValue))
					{
						return overrideValue;
					}
				}

				string storedValue = ConfigurationManager.AppSettings[_AuthorizationKeyAppSettingsKey];

				if (string.IsNullOrEmpty(storedValue))
				{
					return null;
				}
				else
				{
					return storedValue;
				}
			}
		}

		/// <summary>Gets or sets the timeout of callback requests in milliseconds, a value of null indicates a default timeout period.</summary>
		/// <returns>The timeout of callback requests in milliseconds, a value of null indicates a default timeout period.</returns>
		public static int? RequestTimeout
		{
			get
			{
				HttpContext context = HttpContext.Current;

				if (context != null)
				{
					object overrideValue = context.Items[_RequestTimeoutAppSettingsKey];

					if (overrideValue != null && overrideValue is int)
					{
						int overrideInt = (int)overrideValue;

						if (overrideInt >= 0)
						{
							return overrideInt;
						}
					}
				}

				string storedValue = ConfigurationManager.AppSettings[_RequestTimeoutAppSettingsKey];

				if (string.IsNullOrEmpty(storedValue))
				{
					return null;
				}

				int storedInt;

				if (int.TryParse(storedValue, out storedInt))
				{
					if (storedInt >= 0)
					{
						return storedInt;
					}
				}

				return null;
			}
			set
			{
				HttpContext context = HttpContext.Current;
				if (context != null)
				{
					if (value.HasValue && value.Value >= 0)
					{
						context.Items[_RequestTimeoutAppSettingsKey] = value;
					}
					else
					{
						context.Items.Remove(_RequestTimeoutAppSettingsKey);
					}
				}
			}
		}

		/// <summary>Gets or sets the service base Uri used to make callback requests.</summary>
		/// <returns>The service base Uri used to make callback requests.</returns>
		public static Uri ServiceBaseUri
		{
			get
			{
				HttpContext context = HttpContext.Current;

				if (context != null)
				{
					object overrideValue = context.Items[_ServiceBaseUriAppSettingsKey];

					Uri overrideUri = overrideValue as Uri;

					if (overrideUri != null)
					{
						if (overrideUri.IsAbsoluteUri)
						{
							return overrideUri;
						}
					}
				}

				string storedValue = ConfigurationManager.AppSettings[_ServiceBaseUriAppSettingsKey];

				if (string.IsNullOrWhiteSpace(storedValue))
				{
					return null;
				}

				ServiceBaseUri storedUri;

				if (RevaleeClientLibrary::ServiceBaseUri.TryCreate(storedValue, out storedUri))
				{
					return storedUri;
				}

				return null;
			}
			set
			{
				HttpContext context = HttpContext.Current;
				if (context != null)
				{
					if (value != null && value.IsAbsoluteUri)
					{
						context.Items[_ServiceBaseUriAppSettingsKey] = value;
					}
					else
					{
						context.Items.Remove(_ServiceBaseUriAppSettingsKey);
					}
				}
			}
		}
	}
}