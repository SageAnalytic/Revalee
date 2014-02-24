using System;
using System.Net;
using System.Threading;

namespace Revalee.Service
{
	internal class RequestManager : IDisposable
	{
		private HttpListener _Listener;
		private CommandRouter _Router;
		private Thread _ListeningThread;

		public RequestManager()
		{
			if (!HttpListener.IsSupported)
			{
				throw new InvalidOperationException("Windows XP SP2, Windows Server 2003, or later is required to run this service.");
			}

			var requestHandlers = new RequestHandlerDirectory();
			requestHandlers.AddHandler<ScheduleRequestHandler>("Schedule");
			requestHandlers.AddHandler<CancelRequestHandler>("Cancel");
			requestHandlers.AddHandler<StatusRequestHandler>("Status");

			_Router = new CommandRouter(requestHandlers);
			_ListeningThread = new Thread(new ThreadStart(Listen));
		}

		public void Activate()
		{
			if (_Listener == null)
			{
				// Do not commence listening if there are no configured URL prefixes
				if (Supervisor.Configuration.ListenerPrefixes.Length == 0)
				{
					return;
				}

				_Listener = new HttpListener();

				foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
				{
					_Listener.Prefixes.Add(prefix.ToString());
				}

				_Listener.IgnoreWriteExceptions = true;
			}

			try
			{
				_Listener.Start();

				if (!_ListeningThread.IsAlive)
				{
					_ListeningThread.Start();
				}
			}
			catch (HttpListenerException hlex)
			{
				if (hlex.ErrorCode == 5)
				{
					throw new Exception(string.Format("Administrator has not delegated the right to listen on this url prefix [{0}].", Supervisor.Configuration.ListenerPrefixes[0]), hlex);
				}
				else
				{
					throw;
				}
			}
		}

		public void Abort()
		{
			if (_Listener != null)
			{
				_Listener.Abort();
			}
		}

		public void Deactivate()
		{
			if (_Listener != null)
			{
				if (_Listener.IsListening)
				{
					_Listener.Stop();
				}
			}
		}

		private void Listen()
		{
			while (_Listener.IsListening)
			{
				try
				{
					IAsyncResult async_result = _Listener.BeginGetContext(new AsyncCallback(ListenerCallback), _Listener);
					async_result.AsyncWaitHandle.WaitOne();
				}
				catch (HttpListenerException)
				{
					// This exception is thrown by the listener when it is being shutdown
					return;
				}
				catch (ObjectDisposedException)
				{
					// This exception is thrown when the listener is disposed while listening
					return;
				}
			}
		}

		public void ListenerCallback(IAsyncResult result)
		{
			HttpListener listener = (HttpListener)result.AsyncState;
			HttpListenerContext context = null;

			try
			{
				context = listener.EndGetContext(result);
			}
			catch (HttpListenerException)
			{
				// This exception is thrown by the listener when it is being shutdown
				return;
			}
			catch (ObjectDisposedException)
			{
				// This exception is thrown when the listener is disposed while listening
				return;
			}

			if (context == null)
			{
				return;
			}

			ProcessRequest(context.Request, context.Response);
		}

		private void ProcessRequest(HttpListenerRequest request, HttpListenerResponse response)
		{
			response.AddHeader("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0");
			response.AddHeader("Pragma", "no-cache");

			IRequestHandler handler = AssignRequestHandler(request);

			if (handler != null)
			{
				handler.Process(request, response);
			}
			else
			{
				FinalizeIgnoredResponse(response, 404, "Resource Not Found");
			}
		}

		private IRequestHandler AssignRequestHandler(HttpListenerRequest request)
		{
			return _Router.MapHandler(request.RawUrl);
		}

		private void FinalizeIgnoredResponse(HttpListenerResponse response, int statusCode, string statusDescription)
		{
			try
			{
				response.StatusCode = statusCode;
				response.StatusDescription = statusDescription;
			}
			finally
			{
				response.Close();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_Listener != null)
				{
					((IDisposable)_Listener).Dispose();
				}

				if (_ListeningThread != null && _ListeningThread.IsAlive)
				{
					_ListeningThread.Abort();
					_ListeningThread = null;
				}
			}
		}
	}
}