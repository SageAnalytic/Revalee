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
		}

		protected void Application_Error(object sender, EventArgs e)
		{
			Exception error = Server.GetLastError();
			if (error != null)
			{
				System.Diagnostics.Debug.WriteLine("Error: " + error.Message);
			}
		}
	}
}