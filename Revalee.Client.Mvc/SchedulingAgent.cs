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

using Revalee.Client.Validation;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Revalee.Client.Mvc
{
	internal static class SchedulingAgent
	{
		private const int _DefaultRequestTimeoutInMilliseconds = 13000;
		private const string _RevaleeAuthHttpHeaderName = "Revalee-Auth";
		private static readonly Lazy<HttpClient> _LazyHttpClient = new Lazy<HttpClient>(() => InitializeHttpClient(GetWebRequestTimeout()), LazyThreadSafetyMode.ExecutionAndPublication);

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
				throw new ArgumentException("Callback Uri is not an absolute Uri.", "callbackUri");
			}

			var serviceBaseUri = new ServiceBaseUri();

			try
			{
				bool isDisposalRequired;
				HttpClient httpClient = AcquireHttpClient(out isDisposalRequired);

				try
				{
					Uri requestUri = BuildScheduleRequestUri(serviceBaseUri, callbackTime.UtcDateTime, callbackUri);
					string authorizationHeaderValue = RequestValidator.Issue(callbackUri);
					using (var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri))
					{
						if (!string.IsNullOrEmpty(authorizationHeaderValue))
						{
							requestMessage.Headers.Add(_RevaleeAuthHttpHeaderName, authorizationHeaderValue);
						}

						using (HttpResponseMessage response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
						{
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
				}
				finally
				{
					if (isDisposalRequired)
					{
						httpClient.Dispose();
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

		public static Task<bool> CancelCallbackAsync(Guid callbackId, Uri callbackUri)
		{
			return CancelCallbackAsync(callbackId, callbackUri, CancellationToken.None);
		}

		public async static Task<bool> CancelCallbackAsync(Guid callbackId, Uri callbackUri, CancellationToken cancellationToken)
		{
			if (Guid.Empty.Equals(callbackId))
			{
				throw new ArgumentNullException("callbackId");
			}

			if (callbackUri == null)
			{
				throw new ArgumentNullException("callbackUri");
			}

			if (!callbackUri.IsAbsoluteUri)
			{
				throw new ArgumentException("Callback Uri is not an absolute Uri.", "callbackUri");
			}

			var serviceBaseUri = new ServiceBaseUri();

			try
			{
				bool isDisposalRequired;
				HttpClient httpClient = AcquireHttpClient(out isDisposalRequired);

				try
				{
					Uri requestUri = BuildCancelRequestUri(serviceBaseUri, callbackId, callbackUri);
					using (var requestMessage = new HttpRequestMessage(HttpMethod.Put, requestUri))
					{
						using (HttpResponseMessage response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
						{
							if (response.IsSuccessStatusCode)
							{
								return true;
							}
							else
							{
								throw new RevaleeRequestException(serviceBaseUri, callbackUri,
									new WebException(string.Format("The remote server returned an error: ({0}) {1}.",
										(int)response.StatusCode, response.ReasonPhrase), WebExceptionStatus.ProtocolError));
							}
						}
					}
				}
				finally
				{
					if (isDisposalRequired)
					{
						httpClient.Dispose();
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

		private static Uri BuildScheduleRequestUri(Uri serviceBaseUri, DateTime callbackUtcTime, Uri callbackUri)
		{
			return new Uri(string.Format(CultureInfo.InvariantCulture, "{0}://{1}/Schedule?CallbackTime={2:s}Z&CallbackUrl={3}", serviceBaseUri.Scheme, serviceBaseUri.Authority, callbackUtcTime, PrepareCallbackUrl(callbackUri)), UriKind.Absolute);
		}

		private static Uri BuildCancelRequestUri(Uri serviceBaseUri, Guid callbackId, Uri callbackUri)
		{
			return new Uri(string.Format(CultureInfo.InvariantCulture, "{0}://{1}/Cancel?CallbackId={2:D}Z&CallbackUrl={3}", serviceBaseUri.Scheme, serviceBaseUri.Authority, callbackId, PrepareCallbackUrl(callbackUri)), UriKind.Absolute);
		}

		private static string PrepareCallbackUrl(Uri callbackUri)
		{
			return Uri.EscapeDataString(callbackUri.OriginalString);
		}

		private static HttpClient AcquireHttpClient(out bool isDisposalRequired)
		{
			if (_LazyHttpClient.IsValueCreated)
			{
				TimeSpan currentTimeoutSetting = GetWebRequestTimeout();
				HttpClient httpClient = _LazyHttpClient.Value;

				if (httpClient.Timeout == currentTimeoutSetting)
				{
					isDisposalRequired = false;
					return httpClient;
				}
				else
				{
					isDisposalRequired = true;
					return InitializeHttpClient(currentTimeoutSetting);
				}
			}
			else
			{
				isDisposalRequired = false;
				return _LazyHttpClient.Value;
			}
		}

		private static HttpClient InitializeHttpClient(TimeSpan timeout)
		{
			HttpClient httpClient;
			var httpHandler = new HttpClientHandler();

			try
			{
				httpHandler.AllowAutoRedirect = false;
				httpHandler.MaxRequestContentBufferSize = 1024;
				httpHandler.UseCookies = false;
				httpHandler.UseDefaultCredentials = false;

				httpClient = new HttpClient(httpHandler, true);
			}
			catch
			{
				httpHandler.Dispose();
				throw;
			}

			httpClient.DefaultRequestHeaders.ExpectContinue = false;
			httpClient.DefaultRequestHeaders.UserAgent.Clear();
			httpClient.DefaultRequestHeaders.UserAgent.Add(GetUserAgent());
			httpClient.Timeout = timeout;
			httpClient.MaxResponseContentBufferSize = 1024;
			return httpClient;
		}

		private static ProductInfoHeaderValue GetUserAgent()
		{
			Assembly callingAssembly = Assembly.GetCallingAssembly();
			AssemblyName assemblyName = callingAssembly.GetName();
			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(callingAssembly.Location);
			return new ProductInfoHeaderValue(assemblyName.Name, versionInfo.ProductVersion);
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