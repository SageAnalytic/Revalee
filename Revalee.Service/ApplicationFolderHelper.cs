using System;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Revalee.Service
{
	internal static class ApplicationFolderHelper
	{
		private const string _CommonOrganizationFolderName = "Revalee";
		private static string _ApplicationFolderName;
		private static object _SyncLock = new Object();

		public static string ApplicationFolderName
		{
			get
			{
				if (_ApplicationFolderName == null)
				{
					lock (_SyncLock)
					{
						if (_ApplicationFolderName == null)
						{
							_ApplicationFolderName = EnsureApplicationFolder();
						}
					}
				}

				return _ApplicationFolderName;
			}
		}

		private static string EnsureApplicationFolder()
		{
			string commonAppPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

			if (!Directory.Exists(commonAppPath))
			{
				return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			}

			string applicationName = Assembly.GetEntryAssembly().GetName().Name;
			string organizationAppPath = Path.Combine(commonAppPath, _CommonOrganizationFolderName);

			if (!Directory.Exists(organizationAppPath))
			{
				DirectoryInfo orgAppPathInfo = Directory.CreateDirectory(organizationAppPath);
				SetSecurityRights(orgAppPathInfo);
			}

			string applicationAppPath = Path.Combine(organizationAppPath, applicationName);

			if (!Directory.Exists(applicationAppPath))
			{
				Directory.CreateDirectory(applicationAppPath);
			}

			return applicationAppPath;
		}

		private static void SetSecurityRights(DirectoryInfo directoryInfo)
		{
			bool modified;
			DirectorySecurity directorySecurity = directoryInfo.GetAccessControl();
			SecurityIdentifier securityIdentifier = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
			AccessRule rule = new FileSystemAccessRule(
					securityIdentifier,
					FileSystemRights.Write |
					FileSystemRights.ReadAndExecute |
					FileSystemRights.Modify,
					AccessControlType.Allow);
			directorySecurity.ModifyAccessRule(AccessControlModification.Add, rule, out modified);
			directoryInfo.SetAccessControl(directorySecurity);
		}
	}
}