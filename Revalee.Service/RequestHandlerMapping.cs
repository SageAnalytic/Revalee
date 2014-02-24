using System;

namespace Revalee.Service
{
	internal class RequestHandlerMapping
	{
		public RequestHandlerMapping(string command, Type requestHandlerType)
		{
			if (requestHandlerType == null)
			{
				throw new ArgumentNullException("requestHandlerType");
			}

			if (!typeof(IRequestHandler).IsAssignableFrom(requestHandlerType))
			{
				throw new ArgumentException("Mapped type is not a request handler.", "requestHandlerType");
			}

			this.Command = command ?? string.Empty;
			this.RequestHandlerType = requestHandlerType;
		}

		public string Command { get; private set; }

		public Type RequestHandlerType { get; private set; }
	}
}