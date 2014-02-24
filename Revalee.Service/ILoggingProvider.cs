using System.Diagnostics;

namespace Revalee.Service
{
	public interface ILoggingProvider
	{
		void WriteEntry(string message, TraceEventType severity);
	}
}