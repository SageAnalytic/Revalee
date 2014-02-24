using System.Collections.Generic;

namespace Revalee.Service
{
	internal class RequestHandlerDirectory : List<RequestHandlerMapping>
	{
		public void AddHandler<T>(string command) where T : IRequestHandler
		{
			this.Add(new RequestHandlerMapping(command, typeof(T)));
		}
	}
}