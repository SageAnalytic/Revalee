using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
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

						// Worker thread successful, no retries needed
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
						Supervisor.State.AddTask(task);
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

			Supervisor.Telemetry.RecordWaitTime(CalculateWaitTime(DateTime.UtcNow, task));

			while (task.AttemptCallback())
			{
				try
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

					Task<WebResponse> responseTask = Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, request);

					responseTask.ContinueWith(t =>
					{
						try
						{
							HttpWebResponse response = null;
							try
							{
								response = (HttpWebResponse)t.Result;

								// Check for case where a retry is not necessary (agnostic to success versus failure)
								switch (DetermineResult(response.StatusCode))
								{
									case CallbackResult.Success:
										Supervisor.LogEvent(string.Format("Successful callback to {0}. [{1}]", task.CallbackUrl.OriginalString, task.CallbackId), TraceEventType.Verbose);
										Supervisor.Telemetry.RecordSuccessfulCallback();
										return;

									case CallbackResult.NonretryableError:
										Supervisor.LogEvent(string.Format("Could not callback {0}. [{1}]", task.CallbackUrl.OriginalString, task.CallbackId), TraceEventType.Error);
										Supervisor.Telemetry.RecordFailedCallback();
										return;
								}
							}
							catch (AggregateException aex)
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
							HttpWebResponse failedResponse = (HttpWebResponse)wex.Response;
							if (failedResponse != null)
							{
								switch (DetermineResult(failedResponse.StatusCode))
								{
									case CallbackResult.NonretryableError:
										Supervisor.LogException(wex, TraceEventType.Error, task.CallbackUrl.OriginalString);
										Supervisor.Telemetry.RecordFailedCallback();
										return;
								}
							}
						}
						catch (Exception ex)
						{
							// Nonretryable error
							Supervisor.LogException(ex, TraceEventType.Error, task.CallbackUrl.OriginalString);
							Supervisor.Telemetry.RecordFailedCallback();
							return;
						}

						if (task.AttemptsRemaining > 0)
						{
							Thread.Sleep(1000);
							// Reschedule another attempt after a retryable failure
							ProcessTask(task);
						}
						else
						{
							// Out of attempts
							Supervisor.LogEvent(string.Format("Could not callback {0}. [{1}]", task.CallbackUrl.OriginalString, task.CallbackId), TraceEventType.Error);
							Supervisor.Telemetry.RecordFailedCallback();
						}
					});

					return;
				}
				catch (WebException wex)
				{
					HttpWebResponse response = (HttpWebResponse)wex.Response;
					if (response != null)
					{
						switch (DetermineResult(response.StatusCode))
						{
							case CallbackResult.NonretryableError:
								Supervisor.LogException(wex, TraceEventType.Error, task.CallbackUrl.OriginalString);
								Supervisor.Telemetry.RecordFailedCallback();
								return;
						}
					}
				}
				catch (Exception ex)
				{
					// Nonretryable error
					Supervisor.LogException(ex, TraceEventType.Error, task.CallbackUrl.OriginalString);
					Supervisor.Telemetry.RecordFailedCallback();
					return;
				}

				if (task.AttemptsRemaining > 0)
				{
					Thread.Sleep(1000);
				}
				else
				{
					// Out of attempts
					Supervisor.LogEvent(string.Format("Could not callback {0}. [{1}]", task.CallbackUrl.OriginalString, task.CallbackId), TraceEventType.Error);
					Supervisor.Telemetry.RecordFailedCallback();
				}
			}
		}

		public bool IsOverloaded
		{
			get
			{
				int currentFailureCount = _ConsecutiveDoleFailures;

				return (currentFailureCount > 100);
			}
		}

		private static string FormatUtcTime(DateTime time)
		{
			return string.Concat(time.ToString(@"yyyy-MM-dd\THH:mm:ss.fff\Z", CultureInfo.InvariantCulture));
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
			payload.Append(FormatUtcTime(task.CallbackTime));
			payload.Append("&CurrentServiceTime=");
			payload.Append(FormatUtcTime(DateTime.UtcNow));

			return payload.ToString();
		}

		private static string BuildResponseAuthorizationCipher(string requestCipher, Guid callbackId)
		{
			return AuthorizationHelper.ConstructResponse(requestCipher, callbackId);
		}

		private CallbackResult DetermineResult(HttpStatusCode statusCode)
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

				case HttpStatusCode.NotModified:
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