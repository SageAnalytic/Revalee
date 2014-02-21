using System;
using System.Collections.Generic;

namespace RevaleeService
{
	internal class CommandRouter
	{
		private readonly Dictionary<string, RequestHandlerInfo> _HandlerDirectory = new Dictionary<string, RequestHandlerInfo>(StringComparer.OrdinalIgnoreCase);

		internal class RequestHandlerInfo
		{
			public Type RequestHandlerType;
			public IRequestHandler ReentrantInstance;
		}

		public CommandRouter(RequestHandlerDirectory requestHandlers)
		{
			if (requestHandlers == null)
			{
				throw new ArgumentNullException("requestHandlers");
			}

			if (requestHandlers.Count == 0)
			{
				throw new ArgumentException("No request handlers were found in the directory.", "requestHandlers");
			}

			foreach (RequestHandlerMapping mapping in requestHandlers)
			{
				var handlerInfo = new RequestHandlerInfo() { RequestHandlerType = mapping.RequestHandlerType, ReentrantInstance = null };

				try
				{
					_HandlerDirectory.Add(mapping.Command, handlerInfo);
				}
				catch (ArgumentException)
				{
					throw new ArgumentException("The same command was mapped to more than one request handler.", "requestHandlers");
				}
			}
		}

		public IRequestHandler MapHandler(string url)
		{
			RequestHandlerInfo handlerInfo = null;

			if (!_HandlerDirectory.TryGetValue(ParseCommandFromUrl(url), out handlerInfo))
			{
				return null;
			}

			if (handlerInfo == null)
			{
				return null;
			}

			if (handlerInfo.ReentrantInstance != null)
			{
				return handlerInfo.ReentrantInstance;
			}

			IRequestHandler handler = (IRequestHandler)Activator.CreateInstance(handlerInfo.RequestHandlerType);

			if (handler != null && handler.IsReentrant)
			{
				handlerInfo.ReentrantInstance = handler;
			}

			return handler;
		}

		private static string ParseCommandFromUrl(string url)
		{
			if (string.IsNullOrEmpty(url) || url[0] != '/' || url.Length < 2)
			{
				return string.Empty;
			}

			int endPosition = url.IndexOfAny(new char[] { '/', '?' }, 1);

			if (endPosition < 0)
			{
				return url.Substring(1);
			}
			else
			{
				return url.Substring(1, endPosition - 1);
			}
		}
	}
}