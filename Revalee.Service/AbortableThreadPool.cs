using System;
using System.Collections.Generic;
using System.Threading;

namespace Revalee.Service
{
	internal static class AbortableThreadPool
	{
		private readonly static HashSet<WorkItem> _WorkItems = new HashSet<WorkItem>();

		private sealed class WorkItem
		{
			public WaitCallback Callback;
			public object State;
			public ExecutionContext Context;
			public Thread Thread;
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

			// Start tracking this work item
			lock (_WorkItems)
			{
				_WorkItems.Add(workItem);
			}

			try
			{
				// Place work item on the thread pool queue
				success = ThreadPool.QueueUserWorkItem(new WaitCallback(HandleCallback), workItem);
			}
			catch
			{
				// Work item could not be queued
				success = false;
				throw;
			}
			finally
			{
				if (!success)
				{
					// Stop tracking this work item
					RemoveWorkItem(workItem);
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
					// Check if the work item is currently executing
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

						// Remove the ability to abort the work item thread
						workItem.Thread = null;
					}

					// Remove the ability to execute the work item delegate
					workItem.Callback = null;
				}

				// Clear all active work items
				_WorkItems.Clear();
			}
		}

		private static void HandleCallback(object state)
		{
			WorkItem workItem = (WorkItem)state;

			lock (_WorkItems)
			{
				// Check if work item has been cancelled
				if (workItem.Callback == null)
				{
					return;
				}

				// Track the thread assigned to this work item
				workItem.Thread = Thread.CurrentThread;
			}

			try
			{
				// Begin execution of the work item
				ExecutionContext.Run(workItem.Context, new ContextCallback(workItem.Callback.Invoke), workItem.State);
			}
			finally
			{
				// Stop tracking this work item
				RemoveWorkItem(workItem);
			}
		}

		private static void RemoveWorkItem(WorkItem workItem)
		{
			lock (_WorkItems)
			{
				// Remove the ability to execute the work item delegate
				workItem.Callback = null;

				// Remove the ability to abort the work item thread
				workItem.Thread = null;

				// Remove the work item from the list of active work items
				_WorkItems.Remove(workItem);
			}
		}
	}
}