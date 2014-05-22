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
using System.Threading;

namespace Revalee.Client.RecurringTasks
{
	internal class ConfiguredTask : IRecurringTask
	{
		private IClockSource _ClockSource;
		private long _LastOccurrence;

		internal ConfiguredTask(string identifier, IClockSource clockSource, PeriodicityType periodicity, int hourOffset, int minuteOffset, Uri url)
		{
			this.Identifier = identifier;
			this._ClockSource = clockSource;
			this.Periodicity = periodicity;
			this.HourOffset = hourOffset;
			this.MinuteOffset = minuteOffset;
			this.Url = url;
		}

		internal bool HasOccurred
		{
			get
			{
				return (Interlocked.Read(ref _LastOccurrence) != 0L);
			}
		}

		internal long GetNextOccurrence()
		{
			DateTimeOffset now = _ClockSource.Now;
			DateTimeOffset next;

			switch (this.Periodicity)
			{
				case PeriodicityType.Hourly:
					next = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, this.MinuteOffset, 0, now.Offset);

					if (next <= now)
					{
						next = next.AddHours(1.0);
					}

					break;

				case PeriodicityType.Daily:
					next = new DateTimeOffset(now.Year, now.Month, now.Day, this.HourOffset, this.MinuteOffset, 0, now.Offset);

					if (next <= now)
					{
						next = next.AddDays(1.0);
					}

					break;

				default:
					goto case PeriodicityType.Hourly;
			}

			return next.ToUniversalTime().Ticks;
		}

		internal bool SetLastOccurrence(long occurrence)
		{
			do
			{
				long lastOccurrence = Interlocked.Read(ref _LastOccurrence);

				if (lastOccurrence >= occurrence)
				{
					return false;
				}

				if (Interlocked.CompareExchange(ref _LastOccurrence, occurrence, lastOccurrence) == lastOccurrence)
				{
					return true;
				}
			}
			while (true);
		}

		public string Identifier
		{
			get;
			private set;
		}

		public PeriodicityType Periodicity
		{
			get;
			private set;
		}

		public int HourOffset
		{
			get;
			private set;
		}

		public int MinuteOffset
		{
			get;
			private set;
		}

		public Uri Url
		{
			get;
			private set;
		}
	}
}