using System;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace RevaleeService
{
	internal class CancelRequestHandler : IRequestHandler
	{
		public void Process(HttpListenerRequest request, HttpListenerResponse response)
		{
			try
			{
				if (request.HttpMethod != "PUT" && request.HttpMethod != "DELETE")
				{
					FinalizeRejectedResponse(request, response, 405, "Method Not Supported", null);
					return;
				}

				if (request.QueryString.Count < 2)
				{
					FinalizeRejectedResponse(request, response, 400, "Bad Request", null);
					return;
				}

				Guid? guid = RetrieveGuidParameter(request);
				if (!guid.HasValue)
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

				RevaleeTask taskToCancel = RevaleeTask.Revive(DateTime.MinValue, url, DateTime.UtcNow, guid.Value, 0, null);
				Supervisor.State.CancelTask(taskToCancel);

				FinalizeAcceptedResponse(request, response, guid.Value, url);
				return;
			}
			catch (HttpListenerException hlex)
			{
				Supervisor.LogException(hlex, TraceEventType.Error, request.RawUrl);
				FinalizeRejectedResponse(request, response, 500, "Error Occurred", null);
				return;
			}
		}

		private static Guid? RetrieveGuidParameter(HttpListenerRequest request)
		{
			string guidParameter = request.QueryString["CallbackId"];
			Guid guidResult;

			if (Guid.TryParse(guidParameter, out guidResult))
			{
				if (Guid.Empty.Equals(guidResult))
				{
					return null;
				}

				return guidResult;
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

			urlParameter = Uri.EscapeUriString(urlParameter);
			Uri uriResult = null;

			if (Uri.TryCreate(urlParameter, UriKind.Absolute, out uriResult))
			{
				if (!uriResult.IsWellFormedOriginalString())
				{
					return null;
				}

				if (!(Uri.UriSchemeHttp.Equals(uriResult.Scheme) || Uri.UriSchemeHttps.Equals(uriResult.Scheme)))
				{
					return null;
				}

				return uriResult;
			}

			return null;
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

			if (callbackUrl == null)
			{
				Supervisor.LogEvent(string.Format("Cancellation rejected from {0} due to: {1}.", remoteAddress, statusDescription), TraceEventType.Verbose);
			}
			else
			{
				Supervisor.LogEvent(string.Format("Cancellation rejected for {0} from {1} due to: {2}.", callbackUrl.OriginalString, remoteAddress, statusDescription), TraceEventType.Verbose);
			}
		}

		private static void FinalizeAcceptedResponse(HttpListenerRequest request, HttpListenerResponse response, Guid callbackId, Uri callbackUrl)
		{
			string remoteAddress = request.RemoteEndPoint.Address.ToString();

			try
			{
				response.StatusCode = 200;
				response.StatusDescription = "OK";
			}
			finally
			{
				response.Close();
			}

			Supervisor.LogEvent(string.Format("Cancellation processed for {0} from {1}. [{2}]", callbackUrl.OriginalString, remoteAddress, callbackId), TraceEventType.Verbose);
		}

		public bool IsReentrant
		{
			get { return true; }
		}
	}
}