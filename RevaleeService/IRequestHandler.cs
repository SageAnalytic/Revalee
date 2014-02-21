using System.Net;

namespace RevaleeService
{
	internal interface IRequestHandler
	{
		void Process(HttpListenerRequest request, HttpListenerResponse response);

		bool IsReentrant { get; }
	}
}