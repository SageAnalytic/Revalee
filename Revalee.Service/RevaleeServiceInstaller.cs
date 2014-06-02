using System.ComponentModel;
using System.Configuration.Install;

namespace Revalee.Service
{
	[RunInstaller(true)]
	public partial class RevaleeServiceInstaller : Installer
	{
		public RevaleeServiceInstaller()
		{
			InitializeComponent();

			if (!string.Equals(this.serviceInstaller1.ServiceName, Supervisor.Configuration.ServiceName, System.StringComparison.Ordinal))
			{
				RenameService(Supervisor.Configuration.ServiceName);
			}
		}

		private void RenameService(string serviceName)
		{
			this.serviceInstaller1.ServiceName = serviceName;
			this.serviceInstaller1.DisplayName = serviceName.Replace('.', ' ').Replace('_', ' ');
			this.performanceCounterInstaller1.CategoryName = serviceName;
		}
	}
}