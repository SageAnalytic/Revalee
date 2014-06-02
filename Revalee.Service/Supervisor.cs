using System;
using System.Diagnostics;
using System.Reflection;
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
			SupervisorSingleton.Instance.StopInternal(false);
		}

		public static void Shutdown()
		{
			SupervisorSingleton.Instance.StopInternal(true);
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
			LogException(ex, severity, null);
		}

		public static void LogException(Exception ex, TraceEventType severity, string additionalInfo)
		{
			if (ex == null)
			{
				LogExceptionInternal("Abnormal exception occurred.", TraceEventType.Error);
			}
			else if (string.IsNullOrWhiteSpace(additionalInfo))
			{
				LogExceptionInternal(ex.Message, severity);
			}
			else
			{
				LogExceptionInternal(string.Format("{0} [{1}]", ex.Message, additionalInfo), severity);
			}
		}

		private static void LogExceptionInternal(string message, TraceEventType severity)
		{
			var traceSource = new TraceSource(Assembly.GetEntryAssembly().GetName().Name);
			traceSource.TraceEvent(severity, 0, message);
			traceSource.Flush();
			traceSource.Close();
		}

		private void StartInternal()
		{
			lock (_SyncRoot)
			{
				if (!_IsStarted)
				{
					_IsStarted = true;
					_IsPaused = false;
					_TelemetryManager.Activate();
					_StateManager.Initialize();
					_RequestManager.Activate();
					_LoggingProvider.WriteEntry("Revalee service is active and awaiting requests." + GetProductVersionTag(), TraceEventType.Information);
				}
			}
		}

		private void StopInternal(bool isShutdown)
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
					_TelemetryManager.Deactivate();

					if (isShutdown)
					{
						_LoggingProvider.WriteEntry("Revalee service has stopped normally due to a system shutdown." + GetProductVersionTag(), TraceEventType.Information);
					}
					else
					{
						_LoggingProvider.WriteEntry("Revalee service has stopped normally." + GetProductVersionTag(), TraceEventType.Information);
					}

					_LoggingProvider.Flush();
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

			_LoggingProvider.Flush();
		}

		private void ResumeInternal()
		{
			lock (_SyncRoot)
			{
				if (_IsStarted & _IsPaused)
				{
					_ConfigurationManager.ReloadAuthorizedTargets();
					_IsPaused = false;
					_TimeManager.Start();
				}
			}
		}

		private void Cleanup()
		{
			this.StopInternal(true);

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
		}

		public void Dispose()
		{
			this.Cleanup();
			GC.SuppressFinalize(this);
		}

		private static string GetProductVersionTag()
		{
			string assemblyProductVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

			if (string.IsNullOrEmpty(assemblyProductVersion))
			{
				return string.Empty;
			}

			return string.Concat(" (v", assemblyProductVersion, ")");
		}

		private static class SupervisorSingleton
		{
			static internal readonly Supervisor Instance = new Supervisor();
		}
	}
}