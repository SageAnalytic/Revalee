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
using System.Threading;

namespace Revalee.Client.RecurringTasks
{
	internal class ImmutableTaskCollection : ITaskCollection
	{
		private Dictionary<string, ConfiguredTask> _Tasks = new Dictionary<string, ConfiguredTask>();

		internal ImmutableTaskCollection()
		{
		}

		internal ImmutableTaskCollection(IEnumerable<ConfiguredTask> tasks)
		{
			foreach (ConfiguredTask taskConfig in tasks)
			{
				if (!_Tasks.ContainsKey(taskConfig.Identifier))
				{
					_Tasks.Add(taskConfig.Identifier, taskConfig);
				}
			}
		}

		public int Count
		{
			get
			{
				return _Tasks.Count;
			}
		}

		public IEnumerable<ConfiguredTask> Tasks
		{
			get
			{
				return _Tasks.Values;
			}
		}

		public bool TryGetTask(string identifier, out ConfiguredTask taskConfig)
		{
			return _Tasks.TryGetValue(identifier, out taskConfig);
		}

		public bool Add(ConfiguredTask taskConfig)
		{
			// A proper immutable dictionary class would simplify this routine
			do
			{
				Dictionary<string, ConfiguredTask> original = _Tasks;

				if (!original.ContainsKey(taskConfig.Identifier))
				{
					Dictionary<string, ConfiguredTask> clone = new Dictionary<string, ConfiguredTask>(original);

					if (!clone.ContainsKey(taskConfig.Identifier))
					{
						clone.Add(taskConfig.Identifier, taskConfig);

						if (!object.ReferenceEquals(Interlocked.CompareExchange(ref _Tasks, clone, original), original))
						{
							var random = new Random();
							Thread.Sleep(random.Next(50));
							continue;
						}

						return true;
					}
				}

				return false;
			} while (true);
		}

		public bool Remove(string identifier)
		{
			// A proper immutable dictionary class would simplify this routine
			do
			{
				Dictionary<string, ConfiguredTask> original = _Tasks;

				if (original.ContainsKey(identifier))
				{
					Dictionary<string, ConfiguredTask> clone = new Dictionary<string, ConfiguredTask>(original);

					if (clone.ContainsKey(identifier))
					{
						clone.Remove(identifier);

						if (!object.ReferenceEquals(Interlocked.CompareExchange(ref _Tasks, clone, original), original))
						{
							var random = new Random();
							Thread.Sleep(random.Next(50));
							continue;
						}

						return true;
					}
				}

				return false;
			} while (true);
		}

		public bool Remove(ConfiguredTask taskConfig)
		{
			return Remove(taskConfig.Identifier);
		}
	}
}