using System.ComponentModel;
using System.Configuration.Install;

namespace RevaleeService
{
	[RunInstaller(true)]
	public partial class RevaleeServiceInstaller : Installer
	{
		public RevaleeServiceInstaller()
		{
			InitializeComponent();
		}
	}
}