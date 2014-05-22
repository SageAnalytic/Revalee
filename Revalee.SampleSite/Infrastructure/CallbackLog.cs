using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Revalee.SampleSite.Infrastructure
{
	public class CallbackLog
	{
		private Dictionary<Guid, CallbackDetails> _Log = new Dictionary<Guid, CallbackDetails>();

		internal void Add(Guid callbackId, DateTimeOffset callbackTime, Uri callbackUri, DateTimeOffset clientRequestedTime)
		{
			var details = new CallbackDetails() { CallbackId = callbackId, ScheduledCallbackTime = callbackTime, CallbackUri = callbackUri, ClientRequestedTime = clientRequestedTime };

			lock (_Log)
			{
				_Log.Add(callbackId, details);
			}
		}

		internal bool Update(Guid callbackId, DateTimeOffset currentServiceTime, Uri calledbackUri, string parameterValue, DateTimeOffset clientReceivedTime)
		{
			lock (_Log)
			{
				CallbackDetails details;

				if (_Log.TryGetValue(callbackId, out details))
				{
					Debug.Assert(details.CallbackUri.OriginalString.Equals(calledbackUri.OriginalString));
					details.ServiceProcessedTime = currentServiceTime;
					details.ClientReceivedTime = clientReceivedTime;
					details.ParameterValue = parameterValue;
					return true;
				}

				return false;
			}
		}

		internal List<CallbackDetails> Snapshot()
		{
			lock (_Log)
			{
				return new List<CallbackDetails>(_Log.Values);
			}
		}
	}
}