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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Revalee.Client
{
	public partial class RevaleeRegistrar
	{
		/// <summary>
		/// Schedules a callback after a specified delay asynchronously.
		/// </summary>
		/// <param name="serviceHost">A DNS-style domain name or IP address for the Revalee service.</param>
		/// <param name="callbackDelay">A System.TimeSpan that represents a time interval to delay the callback.</param>
		/// <param name="callbackUri">An absolute URL that will be requested on the callback.</param>
		/// <returns>A System.Threading.Tasks.Task&lt;System.Guid&gt; and when complete, the result will be an identifier for the successfully scheduled callback.</returns>
		public static Task<Guid> ScheduleCallbackAsync(string serviceHost, TimeSpan callbackDelay, Uri callbackUri)
		{
			return ScheduleCallbackAsync(BuildServiceBaseUri(serviceHost), DateTimeOffset.Now.Add(callbackDelay), callbackUri);
		}

		/// <summary>
		/// Schedules a callback after a specified delay asynchronously.
		/// </summary>
		/// <param name="serviceBaseUri">A System.Uri representing the scheme, host, and port for the Revalee service (example: http://localhost:46200).</param>
		/// <param name="callbackDelay">A System.TimeSpan that represents a time interval to delay the callback.</param>
		/// <param name="callbackUri">An absolute URL that will be requested on the callback.</param>
		/// <returns>A System.Threading.Tasks.Task&lt;System.Guid&gt; and when complete, the result will be an identifier for the successfully scheduled callback.</returns>
		public static Task<Guid> ScheduleCallbackAsync(Uri serviceBaseUri, TimeSpan callbackDelay, Uri callbackUri)
		{
			return ScheduleCallbackAsync(serviceBaseUri, DateTimeOffset.Now.Add(callbackDelay), callbackUri);
		}

		/// <summary>
		/// Schedules a callback at a specified time asynchronously.
		/// </summary>
		/// <param name="serviceHost">A DNS-style domain name or IP address for the Revalee service.</param>
		/// <param name="callbackTime">A System.DateTimeOffset that represents the scheduled moment of the callback.</param>
		/// <param name="callbackUri">An absolute URL that will be requested on the callback.</param>
		/// <returns>A System.Threading.Tasks.Task&lt;System.Guid&gt; and when complete, the result will be an identifier for the successfully scheduled callback.</returns>
		public static Task<Guid> ScheduleCallbackAsync(string serviceHost, DateTimeOffset callbackTime, Uri callbackUri)
		{
			return ScheduleCallbackAsync(BuildServiceBaseUri(serviceHost), callbackTime, callbackUri);
		}

		/// <summary>
		/// Schedules a callback at a specified time asynchronously.
		/// </summary>
		/// <param name="serviceBaseUri">A System.Uri representing the scheme, host, and port for the Revalee service (example: http://localhost:46200).</param>
		/// <param name="callbackTime">A System.DateTimeOffset that represents the scheduled moment of the callback.</param>
		/// <param name="callbackUri">An absolute URL that will be requested on the callback.</param>
		/// <returns>A System.Threading.Tasks.Task&lt;System.Guid&gt; and when complete, the result will be an identifier for the successfully scheduled callback.</returns>
		public static Task<Guid> ScheduleCallbackAsync(Uri serviceBaseUri, DateTimeOffset callbackTime, Uri callbackUri)
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

			Task<WebResponse> responseTask = Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse, webRequest.EndGetResponse, webRequest);

			ThreadPool.RegisterWaitForSingleObject((responseTask as IAsyncResult).AsyncWaitHandle, TimeoutCallback, webRequest, webRequest.Timeout, true);

			return responseTask.ContinueWith(task =>
				{
					HttpWebResponse response = (HttpWebResponse)task.Result;
					try
					{
						if (response.StatusCode == HttpStatusCode.OK)
						{
							using (var reader = new StreamReader(response.GetResponseStream()))
							{
								string responseText = reader.ReadToEnd();

								return Guid.ParseExact(responseText, "D");
							}
						}

						return Guid.Empty;
					}
					finally
					{
						if (response != null)
						{
							response.Close();
						}
					}
				});
		}

		/// <summary>
		/// Cancels a previously scheduled callback asynchronously.
		/// </summary>
		/// <param name="serviceHost">A DNS-style domain name or IP address for the Revalee service.</param>
		/// <param name="callbackId">A System.Guid that was previously returned from a scheduled callback.</param>
		/// <param name="callbackUri">An absolute URL that matches the specified URL when originally scheduled.</param>
		/// <returns>A System.Threading.Tasks.Task&lt;bool&gt; and when complete, the result will be true if the cancellation request was accepted, false if not</returns>
		public static Task<bool> CancelCallbackAsync(string serviceHost, Guid callbackId, Uri callbackUri)
		{
			return CancelCallbackAsync(BuildServiceBaseUri(serviceHost), callbackId, callbackUri);
		}

		/// <summary>
		/// Cancels a previously scheduled callback asynchronously.
		/// </summary>
		/// <param name="serviceBaseUri">A System.Uri representing the scheme, host, and port for the Revalee service (example: http://localhost:46200).</param>
		/// <param name="callbackId">A System.Guid that was previously returned from a scheduled callback.</param>
		/// <param name="callbackUri">An absolute URL that matches the specified URL when originally scheduled.</param>
		/// <returns>A System.Threading.Tasks.Task&lt;bool&gt; and when complete, the result will be true if the cancellation request was accepted, false if not</returns>
		public static Task<bool> CancelCallbackAsync(Uri serviceBaseUri, Guid callbackId, Uri callbackUri)
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

			Task<WebResponse> responseTask = Task.Factory.FromAsync<WebResponse>(webRequest.BeginGetResponse, webRequest.EndGetResponse, webRequest);

			ThreadPool.RegisterWaitForSingleObject((responseTask as IAsyncResult).AsyncWaitHandle, TimeoutCallback, webRequest, webRequest.Timeout, true);

			return responseTask.ContinueWith(task =>
			{
				HttpWebResponse response = (HttpWebResponse)task.Result;
				try
				{
					if (response.StatusCode == HttpStatusCode.OK)
					{
						return true;
					}

					return false;
				}
				finally
				{
					if (response != null)
					{
						response.Close();
					}
				}
			});
		}

		private static void TimeoutCallback(object state, bool isTimedOut)
		{
			if (isTimedOut)
			{
				WebRequest request = (WebRequest)state;
				if (state != null)
				{
					request.Abort();
				}
			}
		}
	}
}