using System;
using System.Diagnostics;
using System.Security;
using System.Security.Permissions;

namespace RevaleeService
{
	internal class TraceListenerLoggingProvider : ILoggingProvider
	{
		private InternalLogHandler _Log = new InternalLogHandler();

		public void WriteEntry(string message, TraceEventType severity)
		{
			_Log.TraceSource.TraceEvent(severity, 0, message);
		}

		[HostProtection(SecurityAction.LinkDemand, Resources = HostProtectionResource.ExternalProcessMgmt)]
		private sealed class InternalLogHandler
		{
			[SecuritySafeCritical]
			public InternalLogHandler()
			{
				this.TraceSource = new TraceSource(System.Reflection.Assembly.GetEntryAssembly().GetName().Name);
				AppDomain.CurrentDomain.ProcessExit += new EventHandler(this.CloseOnProcessExit);
			}

			[SecuritySafeCritical]
			private void CloseOnProcessExit(object sender, EventArgs e)
			{
				AppDomain.CurrentDomain.ProcessExit -= new EventHandler(this.CloseOnProcessExit);
				this.TraceSource.Close();
			}

			public TraceSource TraceSource
			{
				get;
				private set;
			}
		}
	}
}