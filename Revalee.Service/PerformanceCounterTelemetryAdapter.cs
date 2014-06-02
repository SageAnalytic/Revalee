using System;
using System.Diagnostics;

namespace Revalee.Service
{
	internal class PerformanceCounterTelemetryAdapter : ITelemetryAdapter
	{
		// Awaiting Tasks : NumberOfItems32
		private readonly PerformanceCounter _AwaitingTasks;

		// Average Wait Time : AverageCount64 (ms)
		private readonly PerformanceCounter _AverageWaitTime;

		// Average Wait Time base : AverageBase
		private readonly PerformanceCounter _AverageWaitTimeBase;

		// Requests/Sec : RateOfCountsPerSecond32
		private readonly PerformanceCounter _RequestsPerSecond;

		// Accepted Requests : NumberOfItems64
		private readonly PerformanceCounter _AcceptedRequests;

		// Rejected Requests : NumberOfItems64
		private readonly PerformanceCounter _RejectedRequests;

		// Callbacks/Sec : RateOfCountsPerSecond32
		private readonly PerformanceCounter _CallbacksPerSecond;

		// Successful Callbacks : NumberOfItems64
		private readonly PerformanceCounter _SuccessfulCallbacks;

		// Failed Callbacks : NumberOfItems64
		private readonly PerformanceCounter _FailedCallbacks;

		public PerformanceCounterTelemetryAdapter()
		{
			string categoryName = Supervisor.Configuration.ServiceName;
			_AwaitingTasks = new PerformanceCounter(categoryName, "Awaiting Tasks", false);
			_AverageWaitTime = new PerformanceCounter(categoryName, "Average Wait Time", false);
			_AverageWaitTimeBase = new PerformanceCounter(categoryName, "Average Wait Time base", false);
			_RequestsPerSecond = new PerformanceCounter(categoryName, "Requests/Sec", false);
			_AcceptedRequests = new PerformanceCounter(categoryName, "Accepted Requests", false);
			_RejectedRequests = new PerformanceCounter(categoryName, "Rejected Requests", false);
			_CallbacksPerSecond = new PerformanceCounter(categoryName, "Callbacks/Sec", false);
			_SuccessfulCallbacks = new PerformanceCounter(categoryName, "Successful Callbacks", false);
			_FailedCallbacks = new PerformanceCounter(categoryName, "Failed Callbacks", false);
		}

		public void SetAwaitingTasksValue(int count)
		{
			_AwaitingTasks.RawValue = count;
		}

		public void IncrementAwaitingTasksValue()
		{
			_AwaitingTasks.Increment();
		}

		public void DecrementAwaitingTasksValue()
		{
			_AwaitingTasks.Decrement();
		}

		public void RecordAcceptedRequest()
		{
			_AcceptedRequests.Increment();
			_RequestsPerSecond.Increment();
		}

		public void RecordRejectedRequest()
		{
			_RejectedRequests.Increment();
			_RequestsPerSecond.Increment();
		}

		public void RecordSuccessfulCallback()
		{
			_SuccessfulCallbacks.Increment();
			_CallbacksPerSecond.Increment();
		}

		public void RecordFailedCallback()
		{
			_FailedCallbacks.Increment();
			_CallbacksPerSecond.Increment();
		}

		public void RecordWaitTime(TimeSpan waitTime)
		{
			_AverageWaitTime.IncrementBy(Convert.ToInt64(waitTime.TotalMilliseconds));
			_AverageWaitTimeBase.Increment();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_AwaitingTasks.Dispose();
				_AverageWaitTime.Dispose();
				_AverageWaitTimeBase.Dispose();
				_RequestsPerSecond.Dispose();
				_AcceptedRequests.Dispose();
				_RejectedRequests.Dispose();
				_CallbacksPerSecond.Dispose();
				_SuccessfulCallbacks.Dispose();
				_FailedCallbacks.Dispose();
			}
		}
	}
}