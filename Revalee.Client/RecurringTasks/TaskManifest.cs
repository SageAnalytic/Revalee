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
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Revalee.Client.RecurringTasks
{
	internal class TaskManifest : ITaskManifest
	{
		private static RequestAnalysis NonRecurringRequest = new RequestAnalysis();
		internal const string RecurringTaskHandlerPath = "/__RevaleeRecurring.axd/";

		private readonly Guid _Id = Guid.NewGuid();
		private readonly ActivationState _CurrentState = new ActivationState(false);
		private readonly IClockSource _ClockSource = SystemClock.Instance;
		private readonly string _RecurringTaskHandlerAbsolutePath = GetHandlerAbsolutePath();
		private readonly ITaskCollection _TaskCollection;
		private Uri _CallbackBaseUri;
		private Timer _HeartbeatTimer;
		private int _HeartbeatCount = 0;

		public event EventHandler Activated;

		public event EventHandler<DeactivationEventArgs> Deactivated;

		public event EventHandler<ActivationFailureEventArgs> ActivationFailed;

		internal TaskManifest()
		{
			_TaskCollection = new ImmutableTaskCollection();
		}

		internal TaskManifest(TaskConfigurationCollection configuredTasks)
		{
			if (configuredTasks == null)
			{
				throw new ArgumentNullException("configuredTasks");
			}

			_CallbackBaseUri = configuredTasks.CallbackBaseUri;
			var taskList = new List<ConfiguredTask>();

			using (var taskBuilder = new TaskBuilder(_CallbackBaseUri))
			{
				foreach (TaskConfigurationElement element in configuredTasks)
				{
					taskList.Add(taskBuilder.Create(_ClockSource, element.Periodicity, element.Hour, element.Minute, element.Url));

					if (this.CallbackBaseUri == null)
					{
						this.ScavengeForCallbackBaseUri(element.Url);
					}
				}
			}

			_TaskCollection = new ImmutableTaskCollection(taskList);
		}

		public Uri CallbackBaseUri
		{
			get
			{
				return _CallbackBaseUri;
			}
			set
			{
				Interlocked.Exchange(ref _CallbackBaseUri, value);
			}
		}

		public bool IsActive
		{
			get
			{
				return _CurrentState.IsActive;
			}
		}

		public bool IsEmpty
		{
			get
			{
				return (_TaskCollection.Count == 0);
			}
		}

		public IEnumerable<IRecurringTask> Tasks
		{
			get
			{
				return _TaskCollection.Tasks;
			}
		}

		protected internal bool TryGetTask(string identifier, out ConfiguredTask taskConfig)
		{
			if (string.IsNullOrEmpty(identifier))
			{
				taskConfig = null;
				return false;
			}

			return _TaskCollection.TryGetTask(identifier, out taskConfig);
		}

		protected internal void Start()
		{
			if (this.CallbackBaseUri != null)
			{
				if (!this.IsActive)
				{
					if (_HeartbeatTimer == null)
					{
						// Schedule a heartbeat on a timer
						lock (_TaskCollection)
						{
							if (_HeartbeatTimer == null)
							{
								_HeartbeatTimer = new Timer(delegate(object self)
								{
									try
									{
										if (this.IsActive)
										{
											lock (_TaskCollection)
											{
												if (_HeartbeatTimer != null)
												{
													_HeartbeatTimer.Dispose();
													_HeartbeatTimer = null;
												}
											}
										}
										else
										{
											if (_HeartbeatTimer == null || AppDomain.CurrentDomain.IsFinalizingForUnload())
											{
												return;
											}

											int failureCount = Interlocked.Increment(ref _HeartbeatCount) - 1;

											lock (_TaskCollection)
											{
												if (_HeartbeatTimer != null)
												{
													if (_HeartbeatCount > 20)
													{
														// Leave current timer setting in-place
													}
													else if (_HeartbeatCount > 13)
													{
														_HeartbeatTimer.Change(3600000, 14400000);
													}
													else if (_HeartbeatCount > 3)
													{
														_HeartbeatTimer.Change(60000, 60000);
													}
													else if (_HeartbeatCount > 2)
													{
														_HeartbeatTimer.Change(49750, 60000);
													}
												}
											}

											if (failureCount > 0)
											{
												OnActivationFailure(failureCount);
											}

											try
											{
												RevaleeRegistrar.ScheduleCallback(_ClockSource.Now, this.GenerateHeartbeatCallbackUri());
											}
											catch (RevaleeRequestException)
											{
												// Ignore network errors and retry based on the timer schedule
											}
										}
									}
									catch (AppDomainUnloadedException)
									{
										// Ignore errors when already shutting down
									}
									catch (ObjectDisposedException)
									{
										// Ignore errors when already shutting down
									}
									catch (ThreadAbortException)
									{
										// Ignore errors when already shutting down
									}
								}, null, 250, 10000);
							}
						}
					}
					else
					{
						// Schedule an on-demand heartbeat
						this.Schedule(this.PrepareHeartbeat());
					}
				}
			}
		}

		void ITaskManifest.AddDailyTask(int hour, int minute, Uri url)
		{
			if (hour < 0 || hour > 23)
			{
				throw new ArgumentOutOfRangeException("hour");
			}

			if (minute < 0 || minute > 59)
			{
				throw new ArgumentOutOfRangeException("minute");
			}

			if (this.CallbackBaseUri == null && !url.IsAbsoluteUri)
			{
				throw new ArgumentException("URL must be absolute if no CallbackBaseUri is set.");
			}

			if (url.IsAbsoluteUri && url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
			{
				throw new ArgumentException("Unsupported URL scheme.");
			}

			this.AddTask(_ClockSource, PeriodicityType.Daily, hour, minute, url);
		}

		void ITaskManifest.AddHourlyTask(int minute, Uri url)
		{
			if (minute < 0 || minute > 59)
			{
				throw new ArgumentOutOfRangeException("minute");
			}

			if (this.CallbackBaseUri == null && !url.IsAbsoluteUri)
			{
				throw new ArgumentException("URL must be absolute if no CallbackBaseUri is set.");
			}

			if (url.IsAbsoluteUri && url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
			{
				throw new ArgumentException("Unsupported URL scheme.");
			}

			this.AddTask(_ClockSource, PeriodicityType.Hourly, 0, minute, url);
		}

		void ITaskManifest.RemoveTask(string identifier)
		{
			if (string.IsNullOrEmpty(identifier))
			{
				throw new ArgumentNullException("identifier");
			}

			_TaskCollection.Remove(identifier);
		}

		private void AddTask(IClockSource clockSource, PeriodicityType periodicity, int hour, int minute, Uri url)
		{
			using (var taskBuilder = new TaskBuilder(this.CallbackBaseUri))
			{
				ConfiguredTask taskConfig = taskBuilder.Create(clockSource, periodicity, hour, minute, url);

				if (_TaskCollection.Add(taskConfig))
				{
					if (this.CallbackBaseUri == null)
					{
						this.ScavengeForCallbackBaseUri(taskConfig.Url);
					}

					this.Schedule(this.PrepareNextCallback(taskConfig));

					if (!_CurrentState.IsActive)
					{
						Start();
					}
				}
			}
		}

		internal RequestAnalysis AnalyzeRequest(HttpRequest request)
		{
			string absolutePath = request.Url.AbsolutePath;

			if (absolutePath.StartsWith(_RecurringTaskHandlerAbsolutePath, StringComparison.Ordinal))
			{
				var analysis = new RequestAnalysis();
				analysis.IsRecurringTask = true;
				int parameterStartingIndex = _RecurringTaskHandlerAbsolutePath.Length;

				if (absolutePath.Length > parameterStartingIndex)
				{
					// AbsolutePath format:
					// task       -> ~/__RevaleeRecurring.axd/{identifier}/{occurrence}
					// heartbeat  -> ~/__RevaleeRecurring.axd/{heartbeatId}

					int taskParameterDelimiterIndex = absolutePath.IndexOf('/', parameterStartingIndex);

					if (taskParameterDelimiterIndex < 0)
					{
						// no task parameter delimiter

						if ((absolutePath.Length - parameterStartingIndex) == 32)
						{
							Guid heartbeatId;

							if (Guid.TryParseExact(absolutePath.Substring(parameterStartingIndex), "N", out heartbeatId))
							{
								if (heartbeatId.Equals(_Id))
								{
									this.OnActivate();
								}
							}
						}
					}
					else
					{
						// task parameter delimiter present

						if ((absolutePath.Length - taskParameterDelimiterIndex) > 1)
						{
							if (long.TryParse(absolutePath.Substring(taskParameterDelimiterIndex + 1), NumberStyles.None, CultureInfo.InvariantCulture, out analysis.Occurrence))
							{
								analysis.TaskIdentifier = absolutePath.Substring(parameterStartingIndex, taskParameterDelimiterIndex - parameterStartingIndex);
							}
						}
					}
				}

				// If the TaskIdentifier is not set the default will be string.Empty, which will be discarded by the HttpModule

				return analysis;
			}

			return NonRecurringRequest;
		}

		internal void Reschedule(ConfiguredTask taskConfig)
		{
			this.Schedule(this.PrepareNextCallback(taskConfig));
		}

		protected void OnActivate()
		{
			_HeartbeatCount = 0;

			if (_CurrentState.TransitionToActive())
			{
				Trace.TraceInformation("The Revalee recurring task manager has been activated.");

				foreach (ConfiguredTask taskConfig in _TaskCollection.Tasks)
				{
					if (!taskConfig.HasOccurred)
					{
						this.Schedule(this.PrepareNextCallback(taskConfig));
					}
				}

				EventHandler handler = Activated;

				if (handler != null)
				{
					handler(this, new EventArgs());
				}
			}
		}

		protected void OnDeactivate(RevaleeRequestException exception)
		{
			Trace.TraceError("A Revalee recurring task could not be scheduled.");

			if (_CurrentState.TransitionToInactive())
			{
				this.Start();
				EventHandler<DeactivationEventArgs> handler = Deactivated;

				if (handler != null)
				{
					handler(this, new DeactivationEventArgs(exception));
				}
			}
		}

		protected void OnActivationFailure(int failureCount)
		{
			EventHandler<ActivationFailureEventArgs> handler = ActivationFailed;

			if (handler != null)
			{
				handler(this, new ActivationFailureEventArgs(failureCount));
			}
		}

		private void ScavengeForCallbackBaseUri(Uri url)
		{
			if (url.IsAbsoluteUri)
			{
				Uri baseUri;
				string leftPart = url.GetLeftPart(UriPartial.Authority);

				if (Uri.TryCreate(leftPart, UriKind.Absolute, out baseUri))
				{
					this.CallbackBaseUri = baseUri;
				}
			}
		}

		private void Schedule(CallbackRequest callbackDetails)
		{
			Task.Factory.StartNew(() =>
			{
				try
				{
					RevaleeRegistrar.ScheduleCallback(callbackDetails.CallbackTime, callbackDetails.CallbackUri);
				}
				catch (RevaleeRequestException exception)
				{
					this.OnDeactivate(exception);
				}
			});
		}

		private CallbackRequest PrepareHeartbeat()
		{
			Uri callbackUri = this.GenerateHeartbeatCallbackUri();
			DateTimeOffset callbackTime = _ClockSource.Now;
			return new CallbackRequest(callbackTime, callbackUri);
		}

		private Uri GenerateHeartbeatCallbackUri()
		{
			return new Uri(string.Concat(
				this.CallbackBaseUri.Scheme,
				Uri.SchemeDelimiter,
				this.CallbackBaseUri.Authority,
				_RecurringTaskHandlerAbsolutePath,
				_Id.ToString("N")));
		}

		private CallbackRequest PrepareNextCallback(ConfiguredTask taskConfig)
		{
			long occurrence = taskConfig.GetNextOccurrence();
			Uri callbackUri = this.BuildTaskCallbackUri(taskConfig, occurrence);
			DateTimeOffset callbackTime = new DateTimeOffset(occurrence, TimeSpan.Zero);
			return new CallbackRequest(callbackTime, callbackUri);
		}

		private Uri BuildTaskCallbackUri(ConfiguredTask taskConfig, long occurrence)
		{
			if (!taskConfig.Url.IsAbsoluteUri && HttpContext.Current != null && HttpContext.Current.Request != null)
			{
				string callbackUrlLeftPart = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);

				return new Uri(string.Concat(
					callbackUrlLeftPart,
					_RecurringTaskHandlerAbsolutePath,
					taskConfig.Identifier,
					"/",
					occurrence.ToString(CultureInfo.InvariantCulture)
					), UriKind.Absolute);
			}

			return new Uri(string.Concat(
				taskConfig.Url.Scheme,
				Uri.SchemeDelimiter,
				taskConfig.Url.Authority,
				_RecurringTaskHandlerAbsolutePath,
				taskConfig.Identifier,
				"/",
				occurrence.ToString(CultureInfo.InvariantCulture)
				));
		}

		private static string GetHandlerAbsolutePath()
		{
			string virtualRoot = HttpRuntime.AppDomainAppVirtualPath;

			if (string.IsNullOrEmpty(virtualRoot) || virtualRoot[0] == '/')
			{
				return RecurringTaskHandlerPath;
			}

			if (virtualRoot[virtualRoot.Length - 1] == '/')
			{
				return string.Concat(virtualRoot, RecurringTaskHandlerPath.Substring(1));
			}

			return virtualRoot;
		}
	}
}