using System;

namespace Revalee.SampleSite.Infrastructure
{
	public class CallbackDetails
	{
		public Guid CallbackId;
		public Uri CallbackUri;
		public DateTimeOffset ScheduledCallbackTime;
		public DateTimeOffset ClientRequestedTime;
		public DateTimeOffset? ServiceProcessedTime;
		public DateTimeOffset? ClientReceivedTime;
		public string ParameterValue;
	}
}