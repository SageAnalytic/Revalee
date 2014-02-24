using System;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.ServiceProcess;

namespace Revalee.Service
{
	internal sealed class InteractiveExecution
	{
		private const string _RequiredServiceName = "Revalee.Service";

		private InteractiveExecution()
		{
		}

		public static void Run()
		{
			Console.WriteLine("===] REVALEE [===  v{0}", GetVersionNumber());
			Console.WriteLine("                   {0}", GetCopyrightInformation());
			Console.WriteLine();

			if (!CheckInstallation())
			{
				return;
			}

			Console.WriteLine("Loading stored tasks...");

			try
			{

				Supervisor.Start();
			}
			catch (Exception ex)
			{
				Console.WriteLine("Revalee service cannot start due to the following critical error:");
				Console.WriteLine("*  {0}", ex.Message);
				Console.WriteLine();
				Console.WriteLine("Press any key to terminate.");
				Console.ReadKey(true);
				throw;
			}

			try
			{

				if (Supervisor.Configuration.ListenerPrefixes.Length == 0)
				{
					Supervisor.LogEvent("Revalee service is active but is not listening for requests.", TraceEventType.Warning);
					Console.WriteLine("Revalee service is running but is not listening for callback requests.");
					Console.WriteLine("Press any key to terminate.");
				}
				else if (Supervisor.Configuration.ListenerPrefixes.Length == 1)
				{
					Supervisor.LogEvent("Revalee service is active and awaiting requests.", TraceEventType.Information);
					Console.WriteLine("Revalee service is running and listening on {0}.", Supervisor.Configuration.ListenerPrefixes[0]);
					Console.WriteLine("Press any key to terminate.");
				}
				else
				{
					Supervisor.LogEvent("Revalee service is active and awaiting requests.", TraceEventType.Information);
					Console.WriteLine("Revalee service is running and listening on:");

					foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
					{
						Console.WriteLine("    {0}", prefix);
					}

					Console.WriteLine("Press any key to terminate.");
				}

				Console.ReadKey(true);
				Console.WriteLine();
				Console.WriteLine("Service stopping...");
				Console.WriteLine();


				Supervisor.LogEvent("Revalee service has stopped normally.", TraceEventType.Information);
			}
			finally
			{
				try
				{
					Supervisor.Stop();
				}
				catch { }
			}
		}

		public static void Help()
		{
			Console.WriteLine("===] REVALEE [===  v{0}", GetVersionNumber());
			Console.WriteLine("                   {0}", GetCopyrightInformation());
			Console.WriteLine();
			Console.WriteLine("Switches:");
			Console.WriteLine("    -interactive   Run the service from the command line.");
			Console.WriteLine("    -install       Installs the service into the Windows Service Manager.");
			Console.WriteLine("    -uninstall     Uninstalls the service from the Windows Service Manager.");
			Console.WriteLine("    -help          Displays this information.");
		}

		private static string GetVersionNumber()
		{
			try
			{
				return Assembly.GetExecutingAssembly().GetName().Version.ToString();
			}
			catch
			{
				return string.Empty;
			}
		}

		private static string GetCopyrightInformation()
		{
			try
			{
				return ((AssemblyCopyrightAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright.Replace("©", "(c)");
			}
			catch
			{
				return string.Empty;
			}
		}

		private static bool CheckInstallation()
		{
			if (IsServiceInstalled())
			{
				return true;
			}
			else if (HasRequiredInstallationPermission())
			{
				Console.WriteLine();
				Console.WriteLine("*  The Revalee service cannot run interactively until it is installed.");
				Console.Write("*  Would you like to install it now? [y/n] >");
				ConsoleKeyInfo keypress = Console.ReadKey(false);

				if (keypress.Key == ConsoleKey.Y)
				{
					CommandLineInstaller installer = new CommandLineInstaller();
					installer.Install();

					Console.WriteLine();
					Console.WriteLine();
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				Console.WriteLine();
				Console.WriteLine("*  The Revalee service must be installed before it can be run interactively.");
				Console.WriteLine("*  However, the current permission level is not high enough to install now.");
				Console.WriteLine("*  Please run this executable again with elevated privileges to install.");
				return false;
			}
		}

		private static bool IsServiceInstalled()
		{
			ServiceController serviceController = new ServiceController(_RequiredServiceName);
			try
			{
				string ServiceName = serviceController.ServiceName;
				return true;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
			finally
			{
				serviceController.Close();
			}
		}

		private static bool HasRequiredInstallationPermission()
		{
			if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)))
			{
				return false;
			}
			try
			{
				EventLog.SourceExists(Assembly.GetExecutingAssembly().GetName().Name);
				return true;
			}
			catch (SecurityException)
			{
				return false;
			}
		}
	}
}