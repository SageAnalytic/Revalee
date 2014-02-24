using System;
using System.Diagnostics;

namespace Revalee.Service
{
	internal class TelemetryManager : IDisposable
	{
		private const string _CategoryName = "Revalee.Service";

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

		public TelemetryManager()
		{
			_AwaitingTasks = new PerformanceCounter(_CategoryName, "Awaiting Tasks", false);
			_AverageWaitTime = new PerformanceCounter(_CategoryName, "Average Wait Time", false);
			_AverageWaitTimeBase = new PerformanceCounter(_CategoryName, "Average Wait Time base", false);
			_RequestsPerSecond = new PerformanceCounter(_CategoryName, "Requests/Sec", false);
			_AcceptedRequests = new PerformanceCounter(_CategoryName, "Accepted Requests", false);
			_RejectedRequests = new PerformanceCounter(_CategoryName, "Rejected Requests", false);
			_CallbacksPerSecond = new PerformanceCounter(_CategoryName, "Callbacks/Sec", false);
			_SuccessfulCallbacks = new PerformanceCounter(_CategoryName, "Successful Callbacks", false);
			_FailedCallbacks = new PerformanceCounter(_CategoryName, "Failed Callbacks", false);
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