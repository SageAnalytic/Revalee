using System;
using System.Collections.Generic;

using System.Threading;

namespace Revalee.Service
{
	internal static class AbortableThreadPool
	{
		private static List<WorkItem> _WorkItems = new List<WorkItem>();

		private class WorkItem
		{
			public WaitCallback Callback;
			public object State;
			public ExecutionContext Context;
			public Thread Thread;
		}

		public static bool QueueUserWorkItem(WaitCallback callback)
		{
			return QueueUserWorkItem(callback, null);
		}

		public static bool QueueUserWorkItem(WaitCallback callback, object state)
		{
			if (callback == null)
			{
				throw new ArgumentNullException("callback");
			}

			WorkItem workItem = new WorkItem()
			{
				Callback = callback,
				State = state,
				Context = ExecutionContext.Capture()
			};

			bool success = false;

			lock (_WorkItems)
			{
				_WorkItems.Add(workItem);

				try
				{
					success = ThreadPool.QueueUserWorkItem(new WaitCallback(HandleCallback), workItem);
				}
				catch
				{
					success = false;
					throw;
				}
				finally
				{
					if (!success)
					{
						try
						{
							_WorkItems.Remove(workItem);
						}
						catch
						{
						}
					}
				}
			}

			return success;
		}

		public static void ForceClose()
		{
			lock (_WorkItems)
			{
				foreach (WorkItem workItem in _WorkItems)
				{
					if (workItem.Thread != null)
					{
						if (workItem.State == null)
						{
							workItem.Thread.Abort();
						}
						else
						{
							workItem.Thread.Abort(workItem.State);
						}
					}
				}

				_WorkItems.Clear();
			}
		}

		private static void HandleCallback(object state)
		{
			WorkItem workItem = (WorkItem)state;

			try
			{
				workItem.Thread = Thread.CurrentThread;
				ExecutionContext.Run(workItem.Context, new ContextCallback(workItem.Callback.Invoke), workItem.State);
			}
			finally
			{
				workItem.Thread = null;

				lock (_WorkItems)
				{
					try
					{
						_WorkItems.Remove(workItem);
					}
					catch
					{
					}
				}
			}
		}
	}
}