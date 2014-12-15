#region License

/*
The MIT License (MIT)

Copyright (c) 2014 Sage Analytic Technologies, LLC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#endregion License

using System;
using System.Collections.Generic;

namespace Revalee.Client.RecurringTasks
{
	/// <summary>
	/// Represents a manifest with information on recurring tasks.
	/// </summary>
	public interface ITaskManifest
	{
		/// <summary>
		/// Gets a <see cref="T:System.Boolean" /> value indicating the activation state of the recurring task module.
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		/// Gets a <see cref="T:System.Boolean" /> value indicating that there are no recurring tasks defined.
		/// </summary>
		bool IsEmpty { get; }

		/// <summary>
		/// Gets or sets the <see cref="T:System.Uri" /> defining the base URL for callbacks.
		/// </summary>
		Uri CallbackBaseUri { get; set; }

		/// <summary>
		/// Gets the enumeration of defined <see cref="T:Revalee.Client.RecurringTasks.IRecurringTask" /> objects.
		/// </summary>
		IEnumerable<IRecurringTask> Tasks { get; }

		/// <summary>
		/// Creates a callback task with a daily recurrence.
		/// </summary>
		/// <param name="hour">A <see cref="T:System.Int32" /> value for the scheduled hour (0-23).</param>
		/// <param name="minute">A <see cref="T:System.Int32" /> value for the scheduled minute (0-59).</param>
		/// <param name="url">A <see cref="T:System.Uri" /> value defining the target of the callback.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="hour" /> is not between 0 and 23.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="minute" /> is not between 0 and 59.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="url" /> is not an absolute URL.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="url" /> contains an unsupported URL scheme.</exception>
		void AddDailyTask(int hour, int minute, Uri url);

		/// <summary>
		/// Creates a callback task with a daily recurrence.
		/// </summary>
		/// <param name="minute">A <see cref="T:System.Int32" /> value for the scheduled minute (0-59).</param>
		/// <param name="url">A <see cref="T:System.Uri" /> value defining the target of the callback.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="minute" /> is not between 0 and 59.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="url" /> is not an absolute URL.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="url" /> contains an unsupported URL scheme.</exception>
		void AddHourlyTask(int minute, Uri url);

		/// <summary>
		/// Removes a recurring callback task.
		/// </summary>
		/// <param name="identifier">The <see cref="Revalee.Client.RecurringTasks.IRecurringTask.Identifier" /> of the task to be removed.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="identifier" /> is null.</exception>
		void RemoveTask(string identifier);

		/// <summary>
		/// An event handler triggered when the recurring task module is activated.
		/// </summary>
		event EventHandler Activated;

		/// <summary>
		/// An event handler triggered when the recurring task module is deactivated.
		/// </summary>
		event EventHandler<DeactivationEventArgs> Deactivated;

		/// <summary>
		/// An event handler triggered when an attempt to activate fails.
		/// </summary>
		event EventHandler<ActivationFailureEventArgs> ActivationFailed;
	}
}