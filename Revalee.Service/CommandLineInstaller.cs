using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;

namespace Revalee.Service
{
	internal class CommandLineInstaller
	{
		private const string _NetworkServiceAccountSID = "S-1-5-20";
		private const string _EveryoneAccountSID = "S-1-1-0";

		private readonly TimeSpan _ServiceControllerTimeout = TimeSpan.FromSeconds(15.0);
		private readonly ConfigurationManager _Configuration = new ConfigurationManager();

		public void Install()
		{
			EnsureRequiredPermissions();

			ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });

			RegisterHttpListenerPrefix();

			SetDefaultDataFolderPermissions();

			ConfigureTaskPersistenceProvider();

#if !DEBUG
			StartService();
#endif
		}

		public void Uninstall()
		{
			EnsureRequiredPermissions();

			StopService();

			UnRegisterHttpListenerPrefix();

			ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
		}

		private void RegisterHttpListenerPrefix()
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 5)
			{
				switch (Environment.OSVersion.Version.Major)
				{
					case 5:
						switch (Environment.OSVersion.Version.Minor)
						{
							case 1:
								// Windows XP
								foreach (ListenerPrefix prefix in _Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("httpcfg.exe", string.Format(CultureInfo.InvariantCulture, "set urlacl /u {0} /a D:(A;;GX;;;{1})", prefix, GetAclAccountSid()));
									LaunchNetShellCommand("httpcfg.exe", string.Format(CultureInfo.InvariantCulture, "set iplisten -i 0.0.0.0:{0}", prefix.Port));
								}
								break;

							case 2:
								// Windows Server 2003
								foreach (ListenerPrefix prefix in _Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("httpcfg.exe", string.Format(CultureInfo.InvariantCulture, "set urlacl /u {0} /a D:(A;;GX;;;{1})", prefix, GetAclAccountSid()));
									LaunchNetShellCommand("httpcfg.exe", string.Format(CultureInfo.InvariantCulture, "set iplisten -i 0.0.0.0:{0}", prefix.Port));
								}
								break;
						}
						break;

					case 6:
						switch (Environment.OSVersion.Version.Minor)
						{
							case 0:
								// Windows Vista, Windows Server 2008
								foreach (ListenerPrefix prefix in _Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("netsh", string.Format(CultureInfo.InvariantCulture, "http add urlacl url={0} user=\"{1}\"", prefix, GetAclAccountName()));
									LaunchNetShellCommand("netsh", string.Format(CultureInfo.InvariantCulture, "http add iplisten ipaddress=0.0.0.0:{0}", prefix));
								}
								break;

							default:
								// Windows 7, Windows Server 2008 R2, Windows 8, Windows Server 2012
								foreach (ListenerPrefix prefix in _Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("netsh", string.Format(CultureInfo.InvariantCulture, "http add urlacl url={0} user=\"{1}\"", prefix, GetAclAccountName()));
								}
								break;
						}
						break;

					default:
						// default behavior for future versions
						foreach (ListenerPrefix prefix in _Configuration.ListenerPrefixes)
						{
							LaunchNetShellCommand("netsh", string.Format(CultureInfo.InvariantCulture, "http add urlacl url={0} user=\"{1}\"", prefix, GetAclAccountName()));
						}
						break;
				}
			}
		}

		private void UnRegisterHttpListenerPrefix()
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 5)
			{
				switch (Environment.OSVersion.Version.Major)
				{
					case 5:
						switch (Environment.OSVersion.Version.Minor)
						{
							case 1:
								// Windows XP
								foreach (ListenerPrefix prefix in _Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("httpcfg.exe", string.Format(CultureInfo.InvariantCulture, "delete urlacl /u {0}", prefix));
									LaunchNetShellCommand("httpcfg.exe", string.Format(CultureInfo.InvariantCulture, "delete iplisten -i 0.0.0.0:{0}", prefix.Port));
								}
								break;

							case 2:
								// Windows Server 2003
								foreach (ListenerPrefix prefix in _Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("httpcfg.exe", string.Format(CultureInfo.InvariantCulture, "delete urlacl /u {0}", prefix));
									LaunchNetShellCommand("httpcfg.exe", string.Format(CultureInfo.InvariantCulture, "delete iplisten -i 0.0.0.0:{0}", prefix.Port));
								}
								break;
						}
						break;

					case 6:
						switch (Environment.OSVersion.Version.Minor)
						{
							case 0:
								// Windows Vista, Windows Server 2008
								foreach (ListenerPrefix prefix in _Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("netsh", string.Format(CultureInfo.InvariantCulture, "http delete urlacl url={0}", prefix));
									LaunchNetShellCommand("netsh", string.Format(CultureInfo.InvariantCulture, "http delete iplisten ipaddress=0.0.0.0:{0}", prefix.Port));
								}
								break;

							default:
								// Windows 7, Windows Server 2008 R2, Windows 8, Windows Server 2012
								foreach (ListenerPrefix prefix in _Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("netsh", string.Format(CultureInfo.InvariantCulture, "http delete urlacl url={0}", prefix));
								}
								break;
						}
						break;

					default:
						// default behavior for future versions
						foreach (ListenerPrefix prefix in _Configuration.ListenerPrefixes)
						{
							LaunchNetShellCommand("netsh", string.Format(CultureInfo.InvariantCulture, "http delete urlacl url={0}", prefix));
						}
						break;
				}
			}
		}

		private void SetDefaultDataFolderPermissions()
		{
			SetSecurityRights(new DirectoryInfo(ApplicationFolderHelper.ApplicationFolderName));
		}

		private static void SetSecurityRights(DirectoryInfo directoryInfo)
		{
			DirectorySecurity directorySecurity = directoryInfo.GetAccessControl(AccessControlSections.Access);

			SecurityIdentifier networkServiceSecurityIdentifier = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null);
			FileSystemAccessRule networkServiceRule = new FileSystemAccessRule(
					networkServiceSecurityIdentifier,
					FileSystemRights.Read
					| FileSystemRights.Write
					| FileSystemRights.CreateFiles
					| FileSystemRights.CreateDirectories
					| FileSystemRights.Delete
					| FileSystemRights.DeleteSubdirectoriesAndFiles
					| FileSystemRights.ListDirectory,
					InheritanceFlags.ContainerInherit
					| InheritanceFlags.ObjectInherit,
					PropagationFlags.None,
					AccessControlType.Allow);

			directorySecurity.AddAccessRule(networkServiceRule);

#if DEBUG

			SecurityIdentifier everyoneSecurityIdentifier = new SecurityIdentifier(WellKnownSidType.WorldSid, null);

			FileSystemAccessRule everyoneRule = new FileSystemAccessRule(
					everyoneSecurityIdentifier,
					FileSystemRights.Read
					| FileSystemRights.Write
					| FileSystemRights.CreateFiles
					| FileSystemRights.CreateDirectories
					| FileSystemRights.Delete
					| FileSystemRights.DeleteSubdirectoriesAndFiles
					| FileSystemRights.ListDirectory,
					InheritanceFlags.ContainerInherit
					| InheritanceFlags.ObjectInherit,
					PropagationFlags.None,
					AccessControlType.Allow);

			directorySecurity.AddAccessRule(everyoneRule);

#endif

			directoryInfo.SetAccessControl(directorySecurity);
		}

		private void EnsureRequiredPermissions()
		{
			if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)))
			{
				throw new SecurityException("Insufficient permissions to perform this action.");
			}

			try
			{
				EventLog.SourceExists(_Configuration.ServiceName);
			}
			catch (SecurityException ex)
			{
				throw new SecurityException("Insufficient permissions to perform this action.", ex);
			}
		}

		private void ConfigureTaskPersistenceProvider()
		{
			// No configurable options currently available
		}

		private void StartService()
		{
			var sc = new ServiceController(_Configuration.ServiceName);

			try
			{
				if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
				{
					sc.Start();
					sc.WaitForStatus(ServiceControllerStatus.Running, _ServiceControllerTimeout);
				}
			}
			catch (System.ServiceProcess.TimeoutException)
			{
				Console.Write(string.Format(CultureInfo.InvariantCulture, "Could not start the {0} service.", _Configuration.ServiceName));
			}
			finally
			{
				sc.Close();
			}
		}

		private void StopService()
		{
			var sc = new ServiceController(_Configuration.ServiceName);

			try
			{
				if (sc.CanStop)
				{
					if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
					{
						sc.Stop();
						sc.WaitForStatus(ServiceControllerStatus.Stopped, _ServiceControllerTimeout);
					}
				}
			}
			catch (System.ServiceProcess.TimeoutException)
			{
				Console.Write(string.Format(CultureInfo.InvariantCulture, "Could not stop the {0} service.", _Configuration.ServiceName));
			}
			finally
			{
				sc.Close();
			}
		}

		private void LaunchNetShellCommand(string command, string arguments)
		{
			Console.WriteLine(command + " " + arguments);

			using (var process = new Process())
			{
				process.StartInfo.FileName = command;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.StartInfo.UseShellExecute = false;
				process.Start();
				Console.Write(process.StandardOutput.ReadToEnd());
				process.WaitForExit();
			}
		}

		private static string GetAclAccountSid()
		{
			string sid;

#if DEBUG
			sid = _EveryoneAccountSID;
#else
			sid = _NetworkServiceAccountSID;
#endif

			return sid;
		}

		private static string GetAclAccountName()
		{
			string accountName;

#if DEBUG
			accountName = (new System.Security.Principal.SecurityIdentifier(_EveryoneAccountSID).Translate(typeof(System.Security.Principal.NTAccount))).ToString();
#else
			accountName = (new System.Security.Principal.SecurityIdentifier(_NetworkServiceAccountSID).Translate(typeof(System.Security.Principal.NTAccount))).ToString();
#endif

			return accountName;
		}
	}
}