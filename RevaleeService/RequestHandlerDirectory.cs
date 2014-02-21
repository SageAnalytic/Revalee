using System.Collections.Generic;

namespace RevaleeService
{
	internal class RequestHandlerDirectory : List<RequestHandlerMapping>
	{
		public void AddHandler<T>(string command) where T : IRequestHandler
		{
			this.Add(new RequestHandlerMapping(command, typeof(T)));
		}
	}
}