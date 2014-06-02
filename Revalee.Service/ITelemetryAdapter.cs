using System;

namespace Revalee.Service
{
	internal interface ITelemetryAdapter : IDisposable
	{
		void DecrementAwaitingTasksValue();

		void IncrementAwaitingTasksValue();

		void RecordAcceptedRequest();

		void RecordFailedCallback();

		void RecordRejectedRequest();

		void RecordSuccessfulCallback();

		void RecordWaitTime(TimeSpan waitTime);

		void SetAwaitingTasksValue(int count);
	}
}