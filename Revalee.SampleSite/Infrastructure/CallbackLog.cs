using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Revalee.SampleSite.Infrastructure
{
	public class CallbackLog
	{
		private List<CallbackDetails> _Log = new List<CallbackDetails>();

		internal void Add(Guid callbackId, DateTimeOffset callbackTime, Uri callbackUri)
		{
			DateTimeOffset now = DateTimeOffset.Now;
			lock (((ICollection)_Log).SyncRoot)
			{
				_Log.Add(new CallbackDetails() { CallbackId = callbackId, ScheduledCallbackTime = callbackTime, CallbackUri = callbackUri, ClientRequestedTime = now });
			}
		}

		internal void Update(Guid callbackId, DateTimeOffset currentServiceTime, Uri calledbackUri, string parameterValue)
		{
			DateTimeOffset now = DateTimeOffset.Now;
			lock (((ICollection)_Log).SyncRoot)
			{
				for (int index = _Log.Count - 1; index >= 0; index--)
				{
					CallbackDetails details = _Log[index];
					if (details.CallbackId.Equals(callbackId))
					{
						Debug.Assert(details.CallbackUri.OriginalString.Equals(calledbackUri.OriginalString));
						details.ServiceProcessedTime = currentServiceTime;
						details.ClientReceivedTime = now;
						details.ParameterValue = parameterValue;
					}
				}
			}
		}

		internal List<CallbackDetails> Snapshot()
		{
			lock (((ICollection)_Log).SyncRoot)
			{
				return new List<CallbackDetails>(_Log);
			}
		}
	}
}