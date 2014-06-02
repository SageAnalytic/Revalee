using System;

namespace Revalee.Service
{
	internal class NullTelemetryAdapter : ITelemetryAdapter
	{
		public void DecrementAwaitingTasksValue()
		{
		}

		public void IncrementAwaitingTasksValue()
		{
		}

		public void RecordAcceptedRequest()
		{
		}

		public void RecordFailedCallback()
		{
		}

		public void RecordRejectedRequest()
		{
		}

		public void RecordSuccessfulCallback()
		{
		}

		public void RecordWaitTime(TimeSpan waitTime)
		{
		}

		public void SetAwaitingTasksValue(int count)
		{
		}

		public void Dispose()
		{
		}
	}
}