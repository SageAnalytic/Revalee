using System;
using System.Collections.Generic;

namespace Revalee.Service
{
	internal static class RetryHeuristics
	{
		private static readonly ScheduledDictionary<string, int> _FailedCallbackLog = new ScheduledDictionary<string, int>();
		private static readonly TimeSpan _FailureTimeWindow = TimeSpan.FromHours(6.0);

		public static TimeSpan OnRetryableFailure(Uri callbackUrl)
		{
			if (callbackUrl == null)
			{
				throw new ArgumentNullException("callbackUrl");
			}

			_FailedCallbackLog.RemoveAllOverdue();
			int retryIndex = _FailedCallbackLog.AddOrReplace(callbackUrl.Authority, (key) => 0, (key, oldValue, oldDue) => oldValue + 1, (key, newValue, oldDue) => DateTime.UtcNow.Add(_FailureTimeWindow));
			return AssignDelayInterval(retryIndex);
		}

		public static void OnSuccess(Uri callbackUrl)
		{
			if (callbackUrl == null)
			{
				throw new ArgumentNullException("callbackUrl");
			}

			int dummy;
			_FailedCallbackLog.TryRemove(callbackUrl.Authority, out dummy);
		}

		private static TimeSpan AssignDelayInterval(int retryIndex)
		{
			if (retryIndex < 0)
			{
				return TimeSpan.Zero;
			}

			IList<TimeSpan> retryIntervals = Supervisor.Configuration.RetryIntervals;

			if (retryIndex >= retryIntervals.Count)
			{
				return retryIntervals[retryIntervals.Count - 1];
			}
			else
			{
				return retryIntervals[retryIndex];
			}
		}
	}
}