using System.Diagnostics;

namespace RevaleeService
{
	public interface ILoggingProvider
	{
		void WriteEntry(string message, TraceEventType severity);
	}
}