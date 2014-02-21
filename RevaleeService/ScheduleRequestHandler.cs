using System;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace RevaleeService
{
	internal class ScheduleRequestHandler : IRequestHandler
	{
		public void Process(HttpListenerRequest request, HttpListenerResponse response)
		{
			try
			{
				if (request.HttpMethod != "PUT")
				{
					FinalizeRejectedResponse(request, response, 405, "Method Not Supported", null);
					return;
				}

				if (request.QueryString.Count < 2)
				{
					FinalizeRejectedResponse(request, response, 400, "Bad Request", null);
					return;
				}

				Uri url = RetrieveUrlParameter(request);
				if (url == null)
				{
					FinalizeRejectedResponse(request, response, 400, "Bad Request", null);
					return;
				}

				DateTime? time = RetrieveTimeParameter(request);
				if (!time.HasValue)
				{
					FinalizeRejectedResponse(request, response, 400, "Bad Request", url);
					return;
				}

				if (UrlContainsDangerousMarkup(url))
				{
					FinalizeRejectedResponse(request, response, 400, "Bad Request", url);
					return;
				}

				RevaleeUrlAuthorization authorization = Supervisor.Configuration.AuthorizedTargets.Match(url);
				if (authorization == null)
				{
					// Keep all authorization errors homogeneous from the client perspective
					RevaleeUrlAuthorization.ObfuscateExecutionTime();
					FinalizeRejectedResponse(request, response, 401, "Unauthorized", url);
					return;
				}

				if (!authorization.IsAuthorizedRequestSource(request.RemoteEndPoint.Address))
				{
					// Keep all authorization errors homogeneous from the client perspective
					RevaleeUrlAuthorization.ObfuscateExecutionTime();
					FinalizeRejectedResponse(request, response, 401, "Unauthorized", url);
					return;
				}

				if (Supervisor.Work.IsOverloaded)
				{
					FinalizeRejectedResponse(request, response, 503, "Service Unavailable", url);
					return;
				}

				string authorizationCipher = RetrieveAuthorizationHeader(request);

				RevaleeTask newTask = new RevaleeTask(time.Value, url, authorization.RetryCount, authorizationCipher);
				Supervisor.State.AddTask(newTask);

				FinalizeAcceptedResponse(request, response, newTask);
				return;
			}
			catch (HttpListenerException hlex)
			{
				Supervisor.LogException(hlex, TraceEventType.Error, request.RawUrl);
				FinalizeRejectedResponse(request, response, 500, "Error Occurred", null);
				return;
			}
		}

		private static DateTime? RetrieveTimeParameter(HttpListenerRequest request)
		{
			string timeParameter = request.QueryString["CallbackTime"];
			DateTime dateResult;

			if (DateTime.TryParse(timeParameter, out dateResult))
			{
				switch (dateResult.Kind)
				{
					case DateTimeKind.Local:
						return dateResult.ToUniversalTime();

					case DateTimeKind.Utc:
						return dateResult;

					default:
						return DateTime.SpecifyKind(dateResult, DateTimeKind.Utc);
				}
			}

			return null;
		}

		private static Uri RetrieveUrlParameter(HttpListenerRequest request)
		{
			string urlParameter = request.QueryString["CallbackUrl"];

			if (string.IsNullOrWhiteSpace(urlParameter))
			{
				return null;
			}

			Uri uriResult = null;

			if (Uri.TryCreate(urlParameter, UriKind.Absolute, out uriResult))
			{
				if (!uriResult.IsWellFormedOriginalString())
				{
					uriResult = EncodeUrlSpaceCharacters(uriResult);

					if (!uriResult.IsWellFormedOriginalString())
					{
						return null;
					}
				}

				if (!(Uri.UriSchemeHttp.Equals(uriResult.Scheme) || Uri.UriSchemeHttps.Equals(uriResult.Scheme)))
				{
					return null;
				}

				return uriResult;
			}

			return null;
		}

		private static string RetrieveAuthorizationHeader(HttpListenerRequest request)
		{
			string authorizationHeader = request.Headers["Revalee-Auth"];

			if (string.IsNullOrWhiteSpace(authorizationHeader))
			{
				return null;
			}

			return authorizationHeader;
		}

		private static Uri EncodeUrlSpaceCharacters(Uri url)
		{
			UriBuilder rebuiltUri = new UriBuilder(url);
			rebuiltUri.Path = rebuiltUri.Path.Replace(" ", "%20");
			if (!string.IsNullOrEmpty(rebuiltUri.Query))
			{
				rebuiltUri.Query = rebuiltUri.Query.Substring(1).Replace(" ", "+");
			}
			return rebuiltUri.Uri;
		}

		private static bool UrlContainsDangerousMarkup(Uri url)
		{
			string[] dangerousElements = new string[] { "<script", "<object", "<embed" };

			Func<string, bool> containsAny = (u) =>
			{
				foreach (string tag in dangerousElements)
				{
					if (u.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return true;
					}
				}

				return false;
			};

			return (containsAny(url.OriginalString) || containsAny(url.ToString()));
		}

		private static void FinalizeRejectedResponse(HttpListenerRequest request, HttpListenerResponse response, int statusCode, string statusDescription, Uri callbackUrl)
		{
			string remoteAddress = request.RemoteEndPoint.Address.ToString();

			try
			{
				response.StatusCode = statusCode;
				response.StatusDescription = statusDescription;
			}
			finally
			{
				response.Close();
			}

			Supervisor.Telemetry.RecordRejectedRequest();
			if (callbackUrl == null)
			{
				Supervisor.LogEvent(string.Format("Request rejected from {0} due to: {1}.", remoteAddress, statusDescription), TraceEventType.Verbose);
			}
			else
			{
				Supervisor.LogEvent(string.Format("Request rejected for {0} from {1} due to: {2}.", callbackUrl.OriginalString, remoteAddress, statusDescription), TraceEventType.Verbose);
			}
		}

		private static void FinalizeAcceptedResponse(HttpListenerRequest request, HttpListenerResponse response, RevaleeTask task)
		{
			string remoteAddress = request.RemoteEndPoint.Address.ToString();

			try
			{
				response.StatusCode = 200;
				response.StatusDescription = "OK";
				byte[] confirmation_number = Encoding.UTF8.GetBytes(task.CallbackId.ToString());
				response.ContentLength64 = confirmation_number.LongLength;
				response.OutputStream.Write(confirmation_number, 0, confirmation_number.Length);
			}
			finally
			{
				response.Close();
			}

			Supervisor.Telemetry.RecordAcceptedRequest();
			Supervisor.LogEvent(string.Format("Request accepted for {0} @ {1:d} {1:t} from {2}. [{3}]", task.CallbackUrl.OriginalString, task.CallbackTime, remoteAddress, task.CallbackId), TraceEventType.Verbose);
		}

		public bool IsReentrant
		{
			get { return true; }
		}
	}
}