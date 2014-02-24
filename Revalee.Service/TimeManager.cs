using System;
using System.Threading;

namespace Revalee.Service
{
	// This class will fire off time-based events

	internal class TimeManager : IDisposable
	{
		private const int _ImmediateTimerInterval = 0;
		private const int _MaximumTimerIntervalInMilliseconds = 3600000;

		private readonly object _SyncRoot = new object();
		private Timer _InternalTimer;

		public TimeManager()
		{
		}

		public void Start()
		{
			if (_InternalTimer == null)
			{
				lock (_SyncRoot)
				{
					if (_InternalTimer == null)
					{
						_InternalTimer = new Timer(new TimerCallback(TimerElapsed), null, _ImmediateTimerInterval, Timeout.Infinite);
					}
					else
					{
						_InternalTimer.Change(_ImmediateTimerInterval, Timeout.Infinite);
					}
				}
			}
			else
			{
				_InternalTimer.Change(_ImmediateTimerInterval, Timeout.Infinite);
			}
		}

		public void Stop()
		{
			if (_InternalTimer != null)
			{
				lock (_SyncRoot)
				{
					if (_InternalTimer != null)
					{
						_InternalTimer.Dispose();
						_InternalTimer = null;
					}
				}
			}
		}

		private void TimerElapsed(object state)
		{
			Supervisor.Work.Dispatch();
			EnsureContinuation();
		}

		private void EnsureContinuation()
		{
			DateTimeOffset? nextTaskTime = Supervisor.State.NextTaskTime;
			if (nextTaskTime.HasValue)
			{
				SetNextDueTime(nextTaskTime.Value);
			}
		}

		private void SetNextDueTime(DateTimeOffset nextTaskTime)
		{
			double millisecondsOfDelay = nextTaskTime.Subtract(DateTimeOffset.Now).TotalMilliseconds;

			int nextTimerInterval;

			if (millisecondsOfDelay >= int.MaxValue)
			{
				nextTimerInterval = _MaximumTimerIntervalInMilliseconds;
			}
			else if (millisecondsOfDelay < int.MinValue)
			{
				nextTimerInterval = _ImmediateTimerInterval;
			}
			else
			{
				nextTimerInterval = (int)millisecondsOfDelay;
			}

			if (nextTimerInterval <= _ImmediateTimerInterval)
			{
				_InternalTimer.Change(_ImmediateTimerInterval, Timeout.Infinite);
			}
			else if (nextTimerInterval >= _MaximumTimerIntervalInMilliseconds)
			{
				_InternalTimer.Change(_MaximumTimerIntervalInMilliseconds, Timeout.Infinite);
			}
			else
			{
				_InternalTimer.Change(nextTimerInterval + 1, Timeout.Infinite);
			}
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
				if (_InternalTimer != null)
				{
					_InternalTimer.Dispose();
				}
			}
		}
	}
}