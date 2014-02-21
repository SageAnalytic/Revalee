using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;

namespace RevaleeService
{
	internal class StatusRequestHandler : IRequestHandler
	{
		public void Process(HttpListenerRequest request, HttpListenerResponse response)
		{
			try
			{
				if (request.HttpMethod != "GET")
				{
					response.StatusCode = 405;
					response.StatusDescription = "Method Not Supported";
					response.Close();
					return;
				}

				string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
				string status = GetStatusDescription();
				string timestamp = DateTime.UtcNow.ToString("s", CultureInfo.InvariantCulture) + "Z";

				FormatJsonResponse(response, version, status, timestamp);
			}
			catch (HttpListenerException hlex)
			{
				Supervisor.LogException(hlex, TraceEventType.Error, request.RawUrl);

				response.StatusCode = 500;
				response.StatusDescription = "Error Occurred";
				response.Close();
			}
		}

		private static string GetStatusDescription()
		{
			if (Supervisor.Work.IsOverloaded)
			{
				return "overloaded";
			}
			else if (!Supervisor.IsStarted)
			{
				return "stopped";
			}
			else if (Supervisor.IsPaused)
			{
				return "paused";
			}
			else if (Supervisor.State.AwaitingTaskCount == 0)
			{
				return "idle";
			}
			else
			{
				return "active";
			}
		}

		private static void FormatJsonResponse(HttpListenerResponse response, string version, string status, string timestamp)
		{
			try
			{
				response.StatusCode = 200;
				response.StatusDescription = "OK";
				response.ContentType = "application/json";

				var messageBuffer = new StringBuilder();
				messageBuffer.Append("{ \"version\": \"");
				messageBuffer.Append(version);
				messageBuffer.Append("\", \"status\": \"");
				messageBuffer.Append(status);
				messageBuffer.Append("\", \"timestamp\": \"");
				messageBuffer.Append(timestamp);
				messageBuffer.Append("\" }");

				byte[] binaryPayload = Encoding.UTF8.GetBytes(messageBuffer.ToString());
				response.ContentLength64 = binaryPayload.LongLength;
				response.OutputStream.Write(binaryPayload, 0, binaryPayload.Length);
			}
			finally
			{
				response.Close();
			}
		}

		public bool IsReentrant
		{
			get { return true; }
		}
	}
}