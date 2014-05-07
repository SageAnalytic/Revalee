using System;
using System.Web.Mvc;
using System.Web.Routing;

namespace Revalee.SampleSite
{
	public class MvcApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			RouteConfig.RegisterRoutes(RouteTable.Routes);

			var manifest = Revalee.Client.RecurringTasks.RecurringTaskModule.GetManifest();

			manifest.Activated += RecurringTasks_Activated;
			manifest.Deactivated += RecurringTasks_Deactivated;
		}

		protected void Application_Error(object sender, EventArgs e)
		{
			Exception error = Server.GetLastError();
			if (error != null)
			{
				System.Diagnostics.Debug.WriteLine("Error: " + error.Message);
			}
		}

		protected void RecurringTasks_Activated(object sender, EventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("ACTIVATED");
		}

		protected void RecurringTasks_Deactivated(object sender, Revalee.Client.RecurringTasks.DeactivationEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("DEACTIVATED: " + e.Exception.Message);
		}
	}
}