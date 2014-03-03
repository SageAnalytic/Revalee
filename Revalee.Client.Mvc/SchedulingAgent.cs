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
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Revalee.Client.Mvc
{
	internal static class SchedulingAgent
	{
		private const int _DefaultRequestTimeoutInMilliseconds = 13000;
		private const string _RevaleeAuthHttpHeaderName = "Revalee-Auth";
		private static readonly string _UserAgent = InitializeUserAgent();

		public static Task<Guid> RequestCallbackAsync(Uri callbackUri, DateTimeOffset callbackTime)
		{
			return RequestCallbackAsync(callbackUri, callbackTime, CancellationToken.None);
		}

		public async static Task<Guid> RequestCallbackAsync(Uri callbackUri, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
			if (callbackUri == null)
			{
				throw new ArgumentNullException("callbackUri");
			}

			if (!callbackUri.IsAbsoluteUri)
			{
				throw new UriFormatException("Callback Uri is not an absolute Uri.");
			}

			var serviceBaseUri = new ServiceBaseUri();

			try
			{
				string requestUrl = BuildScheduleRequestUrl(serviceBaseUri, callbackTime.UtcDateTime, callbackUri);

				string authorizationHeaderValue = RequestValidator.Issue(callbackUri);

				using (var httpClient = new HttpClient())
				{
					httpClient.Timeout = GetWebRequestTimeout();
					httpClient.MaxResponseContentBufferSize = 1024;

					var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUrl);
					requestMessage.Headers.Add("User-Agent", _UserAgent);

					if (!string.IsNullOrEmpty(authorizationHeaderValue))
					{
						requestMessage.Headers.Add(_RevaleeAuthHttpHeaderName, authorizationHeaderValue);
					}

					HttpResponseMessage response = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

					if (response.IsSuccessStatusCode)
					{
						string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
						return Guid.ParseExact(responseText, "D");
					}
					else
					{
						throw new RevaleeRequestException(serviceBaseUri, callbackUri,
							new WebException(string.Format("The remote server returned an error: ({0}) {1}.",
								(int)response.StatusCode, response.ReasonPhrase), WebExceptionStatus.ProtocolError));
					}
				}
			}
			catch (AggregateException aex)
			{
				throw new RevaleeRequestException(serviceBaseUri, callbackUri, aex.Flatten().InnerException);
			}
			catch (WebException wex)
			{
				throw new RevaleeRequestException(serviceBaseUri, callbackUri, wex);
			}
		}

		private static string BuildScheduleRequestUrl(Uri serviceBaseUri, DateTime callbackUtcTime, Uri callbackUri)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}://{1}/Schedule?CallbackTime={2:s}Z&CallbackUrl={3}", serviceBaseUri.Scheme, serviceBaseUri.Authority, callbackUtcTime, PrepareCallbackUrl(callbackUri));
		}

		private static string PrepareCallbackUrl(Uri callbackUri)
		{
			return Uri.EscapeDataString(callbackUri.OriginalString);
		}

		private static string InitializeUserAgent()
		{
			Assembly callingAssembly = Assembly.GetCallingAssembly();
			AssemblyName assemblyName = callingAssembly.GetName();
			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(callingAssembly.Location);
			return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", assemblyName.Name, versionInfo.ProductVersion);
		}

		private static TimeSpan GetWebRequestTimeout()
		{
			int? configuredRequestTimeout = RevaleeClientSettings.RequestTimeout;
			if (configuredRequestTimeout.HasValue)
			{
				return TimeSpan.FromMilliseconds(configuredRequestTimeout.Value);
			}
			else
			{
				return TimeSpan.FromMilliseconds(_DefaultRequestTimeoutInMilliseconds);
			}
		}
	}
}