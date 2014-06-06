using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Revalee.Service
{
	// This class will queue each of the networking threads

	internal class WorkManager : IDisposable
	{
		private int _ConsecutiveDoleFailures = 0;

		private enum CallbackResult
		{
			RetryableError,
			NonretryableError,
			Success
		}

		public void Dispatch()
		{
			RevaleeTask task;
			while ((task = Supervisor.State.DoleTask()) != null)
			{
				for (int retryCount = 0; retryCount <= 4; retryCount++)
				{
					if (AbortableThreadPool.QueueUserWorkItem(new WaitCallback(ProcessTask), task))
					{
						// Clear any previous failures
						_ConsecutiveDoleFailures = 0;

						// Worker thread successful, no intervals needed
						break;
					}

					if (retryCount < 4)
					{
						Thread.Sleep(50);
					}
					else
					{
						// Increment the overload counter
						Interlocked.Increment(ref _ConsecutiveDoleFailures);

						// Add the task back to the state manager
						Supervisor.State.ReenlistTask(task);
					}
				}
			}
		}

		public void Halt()
		{
			AbortableThreadPool.ForceClose();
		}

		private void ProcessTask(object state)
		{
			RevaleeTask task = (RevaleeTask)state;

			if (!task.AttemptCallback())
			{
				return;
			}

			try
			{
				HttpWebRequest request = PrepareWebRequest(task);
				Task<WebResponse> responseTask = Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, request);
				responseTask.ContinueWith(t =>
				{
					try
					{
						HttpWebResponse response = null;
						try
						{
							response = (HttpWebResponse)t.Result;
							ProcessWebResponse(task, response);
						}
						catch (AggregateException aex)
						{
							if (aex.InnerExceptions.Count == 1 && aex.InnerExceptions[0] is WebException)
							{
								ProcessWebException(task, aex.InnerExceptions[0] as WebException);
							}
							else
							{
								aex = aex.Flatten();

								if (aex.InnerExceptions.Count > 0)
								{
									throw aex.InnerExceptions[0];
								}
								else
								{
									throw;
								}
							}
						}
						finally
						{
							if (response != null)
							{
								response.Close();
							}
						}
					}
					catch (WebException wex)
					{
						ProcessWebException(task, wex);
					}
					catch (AggregateException aex)
					{
						// Non-retryable error
						CompleteFailedTask(task, aex);
					}
				});
			}
			catch (WebException wex)
			{
				ProcessWebException(task, wex);
			}
			catch (SecurityException sex)
			{
				// Non-retryable error
				CompleteFailedTask(task, sex);
			}
		}

		private static HttpWebRequest PrepareWebRequest(RevaleeTask task)
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(FormatCallbackRequestUrl(task.CallbackUrl));
			request.AllowAutoRedirect = true;
			request.MaximumAutomaticRedirections = 10;
			request.KeepAlive = false;
			request.Method = "POST";
			request.Pipelined = false;
			request.Timeout = 30000;
			request.UserAgent = "Revalee";
			request.ContentType = "application/x-www-form-urlencoded";

			if (!string.IsNullOrEmpty(task.AuthorizationCipher))
			{
				string responseCipher = BuildResponseAuthorizationCipher(task.AuthorizationCipher, task.CallbackId);

				if (responseCipher != null)
				{
					request.Headers.Add("Revalee-Auth", responseCipher);
				}
			}

			string postedData = FormatFormPayload(task);
			request.ContentLength = postedData.Length;
			using (Stream stream = request.GetRequestStream())
			{
				stream.Write(Encoding.UTF8.GetBytes(postedData), 0, postedData.Length);
			}

			return request;
		}

		private void ProcessWebResponse(RevaleeTask task, HttpWebResponse response)
		{
			switch (DetermineResult(response.StatusCode))
			{
				case CallbackResult.Success:
					CompleteSuccessfulTask(task);
					break;

				case CallbackResult.NonretryableError:
					CompleteFailedTask(task, response.StatusCode);
					break;

				default:
					CompleteRetryableTask(task, response.StatusCode);
					break;
			}
		}

		private void ProcessWebException(RevaleeTask task, WebException wex)
		{
			HttpWebResponse failedResponse = (HttpWebResponse)wex.Response;
			if (failedResponse != null)
			{
				switch (DetermineResult(failedResponse.StatusCode))
				{
					case CallbackResult.NonretryableError:
						CompleteFailedTask(task, failedResponse.StatusCode);
						break;

					default:
						CompleteRetryableTask(task, failedResponse.StatusCode);
						break;
				}
			}
			else
			{
				CompleteRetryableTask(task, wex);
			}
		}

		private static void CompleteSuccessfulTask(RevaleeTask task)
		{
			Supervisor.State.CompleteTask(task);
			Supervisor.LogEvent(string.Format("Successful callback to {0}. [{1}]", task.CallbackUrl.OriginalString, task.CallbackId), TraceEventType.Verbose);
			Supervisor.Telemetry.RecordWaitTime(CalculateWaitTime(DateTime.UtcNow, task));
			Supervisor.Telemetry.RecordSuccessfulCallback();
			RetryHeuristics.OnSuccess(task.CallbackUrl);
		}

		private static void CompleteFailedTask(RevaleeTask task, HttpStatusCode statusCode)
		{
			Supervisor.State.CompleteTask(task);
			Supervisor.LogEvent(string.Format("Unsuccessful callback to {0} due to HTTP status code {1}. [{2}]",
				task.CallbackUrl.OriginalString, (int)statusCode, task.CallbackId), TraceEventType.Error);
			Supervisor.Telemetry.RecordWaitTime(CalculateWaitTime(DateTime.UtcNow, task));
			Supervisor.Telemetry.RecordFailedCallback();
		}

		private static void CompleteFailedTask(RevaleeTask task, Exception exception)
		{
			Supervisor.State.CompleteTask(task);
			Supervisor.LogException(exception, TraceEventType.Error, task.CallbackUrl.OriginalString);
			Supervisor.Telemetry.RecordWaitTime(CalculateWaitTime(DateTime.UtcNow, task));
			Supervisor.Telemetry.RecordFailedCallback();
		}

		private static void CompleteRetryableTask(RevaleeTask task, HttpStatusCode statusCode)
		{
			if (task.AttemptsRemaining > 0)
			{
				RetryTask(task);
			}
			else
			{
				// Out of attempts
				CompleteFailedTask(task, statusCode);
			}
		}

		private static void CompleteRetryableTask(RevaleeTask task, Exception exception)
		{
			if (task.AttemptsRemaining > 0)
			{
				RetryTask(task);
			}
			else
			{
				// Out of attempts
				CompleteFailedTask(task, exception);
			}
		}

		private static void RetryTask(RevaleeTask task)
		{
			// Update the persisted attempt counts
			Supervisor.State.UpdateTask(task);

			// Reenlist task to be retried later
			TimeSpan retryDelay = RetryHeuristics.OnRetryableFailure(task.CallbackUrl);
			DateTime delayedCallbackTime = DateTime.UtcNow.Add(retryDelay);
			Supervisor.State.ReenlistTask(task, delayedCallbackTime);
			Supervisor.LogEvent(string.Format("Retrying callback to {0} after waiting {1}. [{2}]", task.CallbackUrl.OriginalString, retryDelay, task.CallbackId), TraceEventType.Information);
		}

		public bool IsOverloaded
		{
			get
			{
				int currentFailureCount = _ConsecutiveDoleFailures;
				return (currentFailureCount > 100);
			}
		}

		private static string FormatCallbackRequestUrl(Uri callbackUrl)
		{
			return callbackUrl.OriginalString;
		}

		private static string FormatFormPayload(RevaleeTask task)
		{
			var payload = new StringBuilder();
			payload.Append("CallbackId=");
			payload.Append(task.CallbackId.ToString("D"));
			payload.Append("&CallbackTime=");
			payload.Append(task.CallbackTime.ToString(@"yyyy-MM-dd\THH:mm:ss.fff\Z", CultureInfo.InvariantCulture));
			payload.Append("&CurrentServiceTime=");
			payload.Append(DateTime.UtcNow.ToString(@"yyyy-MM-dd\THH:mm:ss.fff\Z", CultureInfo.InvariantCulture));
			return payload.ToString();
		}

		private static string BuildResponseAuthorizationCipher(string requestCipher, Guid callbackId)
		{
			return AuthorizationHelper.ConstructResponse(requestCipher, callbackId);
		}

		private static CallbackResult DetermineResult(HttpStatusCode statusCode)
		{
			switch (statusCode)
			{
				case HttpStatusCode.OK:
					return CallbackResult.Success;

				case HttpStatusCode.Accepted:
					return CallbackResult.Success;

				case HttpStatusCode.BadRequest:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.Created:
					return CallbackResult.Success;

				case HttpStatusCode.ExpectationFailed:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.Forbidden:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.Gone:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.HttpVersionNotSupported:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.LengthRequired:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.MethodNotAllowed:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.NoContent:
					return CallbackResult.Success;

				case HttpStatusCode.NotAcceptable:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.NotFound:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.NotImplemented:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.PartialContent:
					return CallbackResult.Success;

				case HttpStatusCode.PaymentRequired:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.RequestUriTooLong:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.ResetContent:
					return CallbackResult.Success;

				case HttpStatusCode.Unauthorized:
					return CallbackResult.NonretryableError;

				case HttpStatusCode.UnsupportedMediaType:
					return CallbackResult.NonretryableError;

				default:
					return CallbackResult.RetryableError;
			}
		}

		private static TimeSpan CalculateWaitTime(DateTime time, RevaleeTask task)
		{
			if (task.CallbackTime > task.CreatedTime)
			{
				if (task.CallbackTime < time)
				{
					return time.Subtract(task.CallbackTime);
				}
				else
				{
					return TimeSpan.Zero;
				}
			}
			else
			{
				if (task.CreatedTime < time)
				{
					return time.Subtract(task.CreatedTime);
				}
				else
				{
					return TimeSpan.Zero;
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
		}
	}
}