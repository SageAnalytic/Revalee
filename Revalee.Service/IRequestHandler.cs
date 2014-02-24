using System.Net;

namespace Revalee.Service
{
	internal interface IRequestHandler
	{
		void Process(HttpListenerRequest request, HttpListenerResponse response);

		bool IsReentrant { get; }
	}
}