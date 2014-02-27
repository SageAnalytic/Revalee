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
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Web;

namespace Revalee.Client.Mvc
{
	/// <summary>
	/// Helper methods to interface with the Revalee service host.
	/// </summary>
	public static partial class RevaleeRegistrar
	{
		private const int _DefaultRequestTimeoutInMilliseconds = 13000;
		private const string _RevaleeAuthHttpHeaderName = "Revalee-Auth";

		/// <summary>
		/// Schedules a callback after a specified delay.
		/// </summary>
		/// <param name="callbackDelay">A System.TimeSpan that represents a time interval to delay the callback.</param>
		/// <param name="callbackUri">An absolute URL that will be requested on the callback.</param>
		/// <returns>A System.Guid that serves as an identifier for the successfully scheduled callback.</returns>
		public static Guid ScheduleCallback(TimeSpan callbackDelay, Uri callbackUri)
		{
			return ScheduleCallback(new ServiceBaseUri(), DateTimeOffset.Now.Add(callbackDelay), callbackUri);
		}

		/// <summary>
		/// Schedules a callback after a specified delay.
		/// </summary>
		/// <param name="serviceHost">A DNS-style domain name or IP address for the Revalee service.</param>
		/// <param name="callbackDelay">A System.TimeSpan that represents a time interval to delay the callback.</param>
		/// <param name="callbackUri">An absolute URL that will be requested on the callback.</param>
		/// <returns>A System.Guid that serves as an identifier for the successfully scheduled callback.</returns>
		public static Guid ScheduleCallback(string serviceHost, TimeSpan callbackDelay, Uri callbackUri)
		{
			return ScheduleCallback(new ServiceBaseUri(serviceHost), DateTimeOffset.Now.Add(callbackDelay), callbackUri);
		}

		/// <summary>
		/// Schedules a callback after a specified delay.
		/// </summary>
		/// <param name="serviceBaseUri">A System.Uri representing the scheme, host, and port for the Revalee service (example: http://localhost:46200).</param>
		/// <param name="callbackDelay">A System.TimeSpan that represents a time interval to delay the callback.</param>
		/// <param name="callbackUri">An absolute URL that will be requested on the callback.</param>
		/// <returns>A System.Guid that serves as an identifier for the successfully scheduled callback.</returns>
		public static Guid ScheduleCallback(Uri serviceBaseUri, TimeSpan callbackDelay, Uri callbackUri)
		{
			return ScheduleCallback(serviceBaseUri, DateTimeOffset.Now.Add(callbackDelay), callbackUri);
		}

		/// <summary>
		/// Schedules a callback at a specified time.
		/// </summary>
		/// <param name="callbackTime">A System.DateTimeOffset that represents the scheduled moment of the callback.</param>
		/// <param name="callbackUri">An absolute URL that will be requested on the callback.</param>
		/// <returns>A System.Guid that serves as an identifier for the successfully scheduled callback.</returns>
		public static Guid ScheduleCallback(DateTimeOffset callbackTime, Uri callbackUri)
		{
			return ScheduleCallback(new ServiceBaseUri(), callbackTime, callbackUri);
		}

		/// <summary>
		/// Schedules a callback at a specified time.
		/// </summary>
		/// <param name="serviceHost">A DNS-style domain name or IP address for the Revalee service.</param>
		/// <param name="callbackTime">A System.DateTimeOffset that represents the scheduled moment of the callback.</param>
		/// <param name="callbackUri">An absolute URL that will be requested on the callback.</param>
		/// <returns>A System.Guid that serves as an identifier for the successfully scheduled callback.</returns>
		public static Guid ScheduleCallback(string serviceHost, DateTimeOffset callbackTime, Uri callbackUri)
		{
			return ScheduleCallback(new ServiceBaseUri(serviceHost), callbackTime, callbackUri);
		}

		/// <summary>
		/// Schedules a callback at a specified time.
		/// </summary>
		/// <param name="serviceBaseUri">A System.Uri representing the scheme, host, and port for the Revalee service (example: http://localhost:46200).</param>
		/// <param name="callbackTime">A System.DateTimeOffset that represents the scheduled moment of the callback.</param>
		/// <param name="callbackUri">An absolute URL that will be requested on the callback.</param>
		/// <returns>A System.Guid that serves as an identifier for the successfully scheduled callback.</returns>
		public static Guid ScheduleCallback(Uri serviceBaseUri, DateTimeOffset callbackTime, Uri callbackUri)
		{
			if (serviceBaseUri == null)
			{
				throw new ArgumentNullException("serviceBaseUri");
			}

			if (callbackUri == null)
			{
				throw new ArgumentNullException("callbackUri");
			}

			string requestUrl = BuildScheduleRequestUrl(serviceBaseUri, callbackTime.UtcDateTime, callbackUri);

			WebRequest webRequest = CreateRequest(requestUrl);

			string authorizationHeaderValue = RequestValidator.Issue(callbackUri);

			if (!string.IsNullOrEmpty(authorizationHeaderValue))
			{
				webRequest.Headers.Add(_RevaleeAuthHttpHeaderName, authorizationHeaderValue);
			}

			Guid trackingGuid = ProcessScheduledCallbackSync(webRequest);

			if (Guid.Empty.Equals(trackingGuid))
			{
				throw new WebException("Service did not return a valid tracking number.");
			}

			return trackingGuid;
		}

		/// <summary>
		/// Cancels a previously scheduled callback.
		/// </summary>
		/// <param name="callbackId">A System.Guid that was previously returned from a scheduled callback.</param>
		/// <param name="callbackUri">An absolute URL that matches the specified URL when originally scheduled.</param>
		/// <returns>true if the cancellation request was accepted, false if not</returns>
		public static bool CancelCallback(Guid callbackId, Uri callbackUri)
		{
			return CancelCallback(new ServiceBaseUri(), callbackId, callbackUri);
		}

		/// <summary>
		/// Cancels a previously scheduled callback.
		/// </summary>
		/// <param name="serviceHost">A DNS-style domain name or IP address for the Revalee service.</param>
		/// <param name="callbackId">A System.Guid that was previously returned from a scheduled callback.</param>
		/// <param name="callbackUri">An absolute URL that matches the specified URL when originally scheduled.</param>
		/// <returns>true if the cancellation request was accepted, false if not</returns>
		public static bool CancelCallback(string serviceHost, Guid callbackId, Uri callbackUri)
		{
			return CancelCallback(new ServiceBaseUri(serviceHost), callbackId, callbackUri);
		}

		/// <summary>
		/// Cancels a previously scheduled callback.
		/// </summary>
		/// <param name="serviceBaseUri">A System.Uri representing the scheme, host, and port for the Revalee service (example: http://localhost:46200).</param>
		/// <param name="callbackId">A System.Guid that was previously returned from a scheduled callback.</param>
		/// <param name="callbackUri">An absolute URL that matches the specified URL when originally scheduled.</param>
		/// <returns>true if the cancellation request was accepted, false if not</returns>
		public static bool CancelCallback(Uri serviceBaseUri, Guid callbackId, Uri callbackUri)
		{
			if (serviceBaseUri == null)
			{
				throw new ArgumentNullException("serviceBaseUri");
			}

			if (callbackUri == null)
			{
				throw new ArgumentNullException("callbackUri");
			}

			string requestUrl = BuildCancelRequestUrl(serviceBaseUri, callbackId, callbackUri);

			WebRequest webRequest = CreateRequest(requestUrl);

			return ProcessCanceledCallbackSync(webRequest);
		}

		/// <summary>
		/// Validates an incoming callback to ensure that only specifically requested callbacks use this entry point.
		/// </summary>
		/// <param name="request">A System.Web.HttpRequestBase object representing the incoming callback.</param>
		/// <returns>true if the callback was originally requested by this same web application; false if not able to make that determination.</returns>
		public static bool ValidateCallback(HttpRequestBase request)
		{
			if (request == null)
			{
				throw new ArgumentNullException("request");
			}

			if (request.Headers == null || request.Form == null)
			{
				return false;
			}

			string authorizationHeader = request.Headers[_RevaleeAuthHttpHeaderName];

			if (string.IsNullOrWhiteSpace(authorizationHeader))
			{
				return false;
			}

			Guid callbackId;
			if (!Guid.TryParse(request.Form["callbackId"], out callbackId))
			{
				return false;
			}

			return RequestValidator.Validate(authorizationHeader, callbackId, request.Url);
		}

		private static Guid ProcessScheduledCallbackSync(WebRequest Request)
		{
			using (HttpWebResponse response = (HttpWebResponse)Request.GetResponse())
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					using (StreamReader reader = new StreamReader(response.GetResponseStream()))
					{
						string responseText = reader.ReadToEnd();
						return Guid.ParseExact(responseText, "D");
					}
				}
			}

			return Guid.Empty;
		}

		private static bool ProcessCanceledCallbackSync(WebRequest Request)
		{
			using (HttpWebResponse response = (HttpWebResponse)Request.GetResponse())
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					return true;
				}
			}

			return false;
		}

		private static string BuildScheduleRequestUrl(Uri serviceBaseUri, DateTime callbackUtcTime, Uri callbackUrl)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}://{1}/Schedule?CallbackTime={2:s}Z&CallbackUrl={3}", serviceBaseUri.Scheme, serviceBaseUri.Authority, callbackUtcTime, EscapeCallbackUrl(callbackUrl));
		}

		private static string BuildCancelRequestUrl(Uri serviceBaseUri, Guid callbackId, Uri callbackUrl)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}://{1}/Cancel?CallbackId={2:D}&CallbackUrl={3}", serviceBaseUri.Scheme, serviceBaseUri.Authority, callbackId, EscapeCallbackUrl(callbackUrl));
		}

		private static string EscapeCallbackUrl(Uri callbackUrl)
		{
			return Uri.EscapeDataString(callbackUrl.OriginalString);
		}

		private static HttpWebRequest CreateRequest(string url)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.AllowAutoRedirect = true;
			request.MaximumAutomaticRedirections = 10;
			request.KeepAlive = true;
			request.Method = WebRequestMethods.Http.Put;
			request.Pipelined = false;
			request.Timeout = GetWebRequestTimeout();
			request.UserAgent = GetUserAgent();
			request.ContentLength = 0;
			return request;
		}

		private static int GetWebRequestTimeout()
		{
			int? configuredRequestTimeout = RevaleeClientSettings.RequestTimeout;
			if (configuredRequestTimeout.HasValue)
			{
				return configuredRequestTimeout.Value;
			}
			else
			{
				return _DefaultRequestTimeoutInMilliseconds;
			}
		}

		private static string GetUserAgent()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0} v{1}", Assembly.GetCallingAssembly().GetName().Name, Assembly.GetCallingAssembly().GetName().Version.ToString());
		}
	}
}