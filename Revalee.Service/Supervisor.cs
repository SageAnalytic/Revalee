using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;

namespace Revalee.Service
{
	internal sealed class Supervisor : CriticalFinalizerObject, IDisposable
	{
		private readonly ILoggingProvider _LoggingProvider;
		private readonly ConfigurationManager _ConfigurationManager;
		private readonly TelemetryManager _TelemetryManager;
		private readonly StateManager _StateManager;
		private readonly TimeManager _TimeManager;
		private readonly RequestManager _RequestManager;
		private readonly WorkManager _WorkManager;
		private readonly object _SyncRoot = new object();

		private bool _IsStarted;
		private bool _IsPaused;

		private Supervisor()
		{
			try
			{
				_LoggingProvider = new TraceListenerLoggingProvider();
				try
				{
					_ConfigurationManager = new ConfigurationManager();
					_TelemetryManager = new TelemetryManager();
					_StateManager = new StateManager();
					_TimeManager = new TimeManager();
					_RequestManager = new RequestManager();
					_WorkManager = new WorkManager();
				}
				catch (Exception ex2)
				{
					try
					{
						_LoggingProvider.WriteEntry(string.Format("{0} [Critical startup error.]", ex2.Message), TraceEventType.Critical);
					}
					catch (Exception ex3)
					{
						Console.WriteLine("Could not write to the error log.");
						Console.WriteLine("*  {0}", ex3.Message);
					}

					throw;
				}
			}
			catch (Exception ex1)
			{
				Console.WriteLine("Could not initialize logging subsystem.");
				Console.WriteLine("*  {0}", ex1.Message);
				throw;
			}
		}

		~Supervisor()
		{
			this.Dispose();
		}

		public static ConfigurationManager Configuration
		{
			get { return SupervisorSingleton.Instance._ConfigurationManager; }
		}

		private static ILoggingProvider Log
		{
			get { return SupervisorSingleton.Instance._LoggingProvider; }
		}

		public static TelemetryManager Telemetry
		{
			get { return SupervisorSingleton.Instance._TelemetryManager; }
		}

		public static TimeManager Time
		{
			get { return SupervisorSingleton.Instance._TimeManager; }
		}

		public static RequestManager Request
		{
			get { return SupervisorSingleton.Instance._RequestManager; }
		}

		public static StateManager State
		{
			get { return SupervisorSingleton.Instance._StateManager; }
		}

		public static WorkManager Work
		{
			get { return SupervisorSingleton.Instance._WorkManager; }
		}

		public static bool IsStarted
		{
			get
			{
				return SupervisorSingleton.Instance._IsStarted;
			}
		}

		public static bool IsPaused
		{
			get
			{
				return SupervisorSingleton.Instance._IsPaused;
			}
		}

		public static void Start()
		{
			SupervisorSingleton.Instance.StartInternal();
		}

		public static void Stop()
		{
			SupervisorSingleton.Instance.StopInternal();
		}

		public static void Pause()
		{
			SupervisorSingleton.Instance.PauseInternal();
		}

		public static void Resume()
		{
			SupervisorSingleton.Instance.ResumeInternal();
		}

		public static void LogEvent(string message, TraceEventType severity)
		{
			Supervisor.Log.WriteEntry(message, severity);
		}

		public static void LogException(Exception ex, TraceEventType severity)
		{
			if (ex == null)
			{
				Supervisor.Log.WriteEntry("Abnormal exception occurred.", TraceEventType.Error);
			}
			else
			{
				Supervisor.Log.WriteEntry(ex.Message, severity);
			}
		}

		public static void LogException(Exception ex, TraceEventType severity, string additionalInfo)
		{
			if (ex == null)
			{
				Supervisor.Log.WriteEntry("Abnormal exception occurred.", TraceEventType.Error);
			}
			else if (string.IsNullOrWhiteSpace(additionalInfo))
			{
				Supervisor.Log.WriteEntry(ex.Message, severity);
			}
			else
			{
				Supervisor.Log.WriteEntry(string.Format("{0} [{1}]", ex.Message, additionalInfo), severity);
			}
		}

		private void StartInternal()
		{
			lock (_SyncRoot)
			{
				if (!_IsStarted)
				{
					_IsStarted = true;
					_IsPaused = false;
					_StateManager.Initialize();
					_RequestManager.Activate();
				}
			}
		}

		private void StopInternal()
		{
			lock (_SyncRoot)
			{
				if (_IsStarted)
				{
					_IsStarted = false;
					_IsPaused = false;
					_RequestManager.Deactivate();
					_TimeManager.Stop();
					_WorkManager.Halt();
					_StateManager.Suspend();
				}
			}
		}

		private void PauseInternal()
		{
			lock (_SyncRoot)
			{
				if (_IsStarted && !_IsPaused)
				{
					_IsPaused = true;
					_TimeManager.Stop();
				}
			}
		}

		private void ResumeInternal()
		{
			lock (_SyncRoot)
			{
				if (_IsStarted & _IsPaused)
				{
					_IsPaused = false;
					_TimeManager.Start();
				}
			}
		}

		private void Cleanup()
		{
			this.StopInternal();

			if (_RequestManager != null)
			{
				try
				{
					_RequestManager.Dispose();
				}
				catch { }
			}

			if (_TimeManager != null)
			{
				try
				{
					_TimeManager.Dispose();
				}
				catch { }
			}

			if (_WorkManager != null)
			{
				try
				{
					_WorkManager.Dispose();
				}
				catch { }
			}

			if (_StateManager != null)
			{
				try
				{
					_StateManager.Dispose();
				}
				catch { }
			}

			if (_TelemetryManager != null)
			{
				try
				{
					_TelemetryManager.Dispose();
				}
				catch { }
			}

			if (_ConfigurationManager != null)
			{
				try
				{
					_ConfigurationManager.Dispose();
				}
				catch { }
			}
		}

		public void Dispose()
		{
			this.Cleanup();
		}

		private static class SupervisorSingleton
		{
			static internal readonly Supervisor Instance = new Supervisor();
		}
	}
}