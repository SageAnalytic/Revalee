using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Revalee.Service
{
	internal class CommandLineInstaller
	{
		private const string _NetworkServiceAccountSID = "S-1-5-20";
		private const string _EveryoneAccountSID = "S-1-1-0";

		public void Install()
		{
			EnsureRequiredPermissions();

			ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });

			RegisterHttpPrefix();

			SetDefaultDataFolderPermissions();
		}

		public void Uninstall()
		{
			EnsureRequiredPermissions();

			UnRegisterHttpPrefix();

			ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
		}

		private void RegisterHttpPrefix()
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
								foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("httpcfg.exe", string.Format("set urlacl /u {0} /a D:(A;;GX;;;{1})", prefix, GetAclAccountSid()));
									LaunchNetShellCommand("httpcfg.exe", string.Format("set iplisten -i 0.0.0.0:{0}", prefix.Port));
								}
								break;

							case 2:
								// Windows Server 2003
								foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("httpcfg.exe", string.Format("set urlacl /u {0} /a D:(A;;GX;;;{1})", prefix, GetAclAccountSid()));
									LaunchNetShellCommand("httpcfg.exe", string.Format("set iplisten -i 0.0.0.0:{0}", prefix.Port));
								}
								break;
						}
						break;

					case 6:
						switch (Environment.OSVersion.Version.Minor)
						{
							case 0:
								// Windows Vista, Windows Server 2008
								foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("netsh", string.Format("http add urlacl url={0} user=\"{1}\"", prefix, GetAclAccountName()));
									LaunchNetShellCommand("netsh", string.Format("http add iplisten ipaddress=0.0.0.0:{0}", prefix));
								}
								break;

							default:
								// Windows 7, Windows Server 2008 R2, Windows 8, Windows Server 2012
								foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("netsh", string.Format("http add urlacl url={0} user=\"{1}\"", prefix, GetAclAccountName()));
								}
								break;
						}
						break;

					default:
						// default behavior for future versions
						foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
						{
							LaunchNetShellCommand("netsh", string.Format("http add urlacl url={0} user=\"{1}\"", prefix, GetAclAccountName()));
						}
						break;
				}
			}
		}

		private void UnRegisterHttpPrefix()
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
								foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("httpcfg.exe", string.Format("delete urlacl /u {0}", prefix));
									LaunchNetShellCommand("httpcfg.exe", string.Format("delete iplisten -i 0.0.0.0:{0}", prefix.Port));
								}
								break;

							case 2:
								// Windows Server 2003
								foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("httpcfg.exe", string.Format("delete urlacl /u {0}", prefix));
									LaunchNetShellCommand("httpcfg.exe", string.Format("delete iplisten -i 0.0.0.0:{0}", prefix.Port));
								}
								break;
						}
						break;

					case 6:
						switch (Environment.OSVersion.Version.Minor)
						{
							case 0:
								// Windows Vista, Windows Server 2008
								foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("netsh", string.Format("http delete urlacl url={0}", prefix));
									LaunchNetShellCommand("netsh", string.Format("http delete iplisten ipaddress=0.0.0.0:{0}", prefix.Port));
								}
								break;

							default:
								// Windows 7, Windows Server 2008 R2, Windows 8, Windows Server 2012
								foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
								{
									LaunchNetShellCommand("netsh", string.Format("http delete urlacl url={0}", prefix));
								}
								break;
						}
						break;

					default:
						// default behavior for future versions
						foreach (ListenerPrefix prefix in Supervisor.Configuration.ListenerPrefixes)
						{
							LaunchNetShellCommand("netsh", string.Format("http delete urlacl url={0}", prefix));
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
				EventLog.SourceExists(GetServiceName());
			}
			catch (SecurityException ex)
			{
				throw new SecurityException("Insufficient permissions to perform this action.", ex);
			}
		}

		private void LaunchNetShellCommand(string command, string arguments)
		{
			Console.WriteLine(command + " " + arguments);

			Process process = new Process();
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

		private static string GetServiceName()
		{
			return Assembly.GetExecutingAssembly().GetName().Name;
		}
	}
}