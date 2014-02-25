using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;

namespace Revalee.Service
{
	partial class Revalee : ServiceBase
	{
		public Revalee()
		{
			InitializeComponent();
		}

		protected override void OnContinue()
		{
			Supervisor.Resume();
		}

		protected override void OnPause()
		{
			Supervisor.Pause();
		}

		protected override void OnStart(string[] args)
		{
			try
			{
				Supervisor.Start();
				Supervisor.LogEvent("Revalee service is active and awaiting requests.", TraceEventType.Information);
			}
			catch (Exception ex)
			{
				Supervisor.LogException(ex, TraceEventType.Critical, "Service failed to start");

				using (var controller = new ServiceController(this.ServiceName))
				{
					controller.Stop();
				}

				this.ExitCode = 1;
			}
		}

		protected override void OnStop()
		{
			try
			{
				Supervisor.Stop();
				Supervisor.LogEvent("Revalee service has stopped normally.", TraceEventType.Information);
			}
			catch { }
		}

		[MTAThread]
		public static void Main()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			if (commandLineArgs != null && commandLineArgs.Length == 2)
			{
				if (commandLineArgs[1] == "-?" || string.Equals(commandLineArgs[1], "-help", StringComparison.OrdinalIgnoreCase))
				{
					InteractiveExecution.Help();
					return;
				}
				else if (string.Equals(commandLineArgs[1], "-install", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						CommandLineInstaller installer = new CommandLineInstaller();
						installer.Install();
					}
					catch (Exception ex)
					{
						Environment.ExitCode = 1;
						Console.WriteLine();
						Console.WriteLine(ex.Message);
					}

					return;
				}
				else if (string.Equals(commandLineArgs[1], "-uninstall", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						CommandLineInstaller installer = new CommandLineInstaller();
						installer.Uninstall();
					}
					catch (Exception ex)
					{
						Environment.ExitCode = 1;
						Console.WriteLine();
						Console.WriteLine(ex.Message);
					}

					return;
				}
				else if (string.Equals(commandLineArgs[1], "-interactive", StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						InteractiveExecution.Run();
					}
					catch (Exception ex)
					{
						try
						{
							Supervisor.LogException(ex, TraceEventType.Critical, "Service terminating on error");
						}
						catch { }

						Environment.ExitCode = 1;
					}
					return;
				}
				else if (string.Equals(commandLineArgs[1], "-export", StringComparison.OrdinalIgnoreCase))
				{
					TaskExporter.DumpToConsole();
					return;
				}
			}

			try
			{
				AppDomain.CurrentDomain.UnhandledException += ServiceCallbackUnhandledExceptionHandler;
				ServiceBase.Run(new Revalee());
			}
			catch (Exception ex)
			{
				try
				{
					Supervisor.LogException(ex, TraceEventType.Critical, "Service encountered a critical error");
				}
				catch
				{ }
			}
		}

		private static void ServiceCallbackUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
		{
			try
			{
				Supervisor.LogException((Exception)args.ExceptionObject, TraceEventType.Critical, "Service encountered a critical error");
			}
			catch
			{ }
		}
	}
}