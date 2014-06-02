using System;
using System.Diagnostics;

namespace Revalee.Service
{
	internal class TelemetryManager : ITelemetryAdapter, IDisposable
	{
		private static readonly ITelemetryAdapter _EmptyAdapter = new NullTelemetryAdapter();
		private ITelemetryAdapter _Adapter;

		public TelemetryManager()
		{
			_Adapter = _EmptyAdapter;
		}

		public void Activate()
		{
			if (_Adapter == _EmptyAdapter)
			{
				Activate(new PerformanceCounterTelemetryAdapter());
			}
		}

		public void Activate(ITelemetryAdapter telemetryAdapter)
		{
			if (telemetryAdapter == null)
			{
				throw new ArgumentNullException("telemetryAdapter");
			}

			if (_Adapter == _EmptyAdapter)
			{
				_Adapter = telemetryAdapter;
			}
		}

		public void Deactivate()
		{
			_Adapter = _EmptyAdapter;
		}

		public void SetAwaitingTasksValue(int count)
		{
			_Adapter.SetAwaitingTasksValue(count);
		}

		public void IncrementAwaitingTasksValue()
		{
			_Adapter.IncrementAwaitingTasksValue();
		}

		public void DecrementAwaitingTasksValue()
		{
			_Adapter.DecrementAwaitingTasksValue();
		}

		public void RecordAcceptedRequest()
		{
			_Adapter.RecordAcceptedRequest();
		}

		public void RecordRejectedRequest()
		{
			_Adapter.RecordRejectedRequest();
		}

		public void RecordSuccessfulCallback()
		{
			_Adapter.RecordSuccessfulCallback();
		}

		public void RecordFailedCallback()
		{
			_Adapter.RecordFailedCallback();
		}

		public void RecordWaitTime(TimeSpan waitTime)
		{
			_Adapter.RecordWaitTime(waitTime);
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
				_Adapter.Dispose();
			}
		}
	}
}