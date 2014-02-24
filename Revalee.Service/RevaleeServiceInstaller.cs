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
		}
	}
}