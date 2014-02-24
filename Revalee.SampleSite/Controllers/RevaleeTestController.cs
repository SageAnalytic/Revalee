using Revalee.SampleSite.Infrastructure;
using Revalee.Client;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Revalee.SampleSite.Controllers
{
	public class RevaleeTestController : Controller
	{
		private static CallbackLog _Log = new CallbackLog();

		private static int _TotalRequestCount = 0;

		private static int _TotalCallbackCount = 0;

		public ActionResult Index()
		{
			return View();
		}

		public ActionResult ViewLog()
		{
			ViewBag.TotalRequestCount = _TotalRequestCount;

			ViewBag.TotalCallbackCount = _TotalCallbackCount;

			return this.PartialView(_Log.Snapshot());
		}

		public ActionResult Schedule(Uri serviceBaseUri, DateTimeOffset callbackTime, Uri callbackUri)
		{
			Interlocked.Increment(ref _TotalRequestCount);

			try
			{
				Guid callbackId = RevaleeRegistrar.ScheduleCallback(serviceBaseUri, callbackTime, callbackUri);
				_Log.Add(callbackId, callbackTime, callbackUri);
				return this.Content("OK");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				return this.Content("Error:" + ex.Message);
			}
		}

		public async Task<ActionResult> ScheduleAsync(Uri serviceBaseUri, DateTimeOffset callbackTime, Uri callbackUri)
		{
			Interlocked.Increment(ref _TotalRequestCount);

			try
			{
				Guid callbackId = await RevaleeRegistrar.ScheduleCallbackAsync(serviceBaseUri, callbackTime, callbackUri);
				_Log.Add(callbackId, callbackTime, callbackUri);
				return this.Content("OK");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				return this.Content("Error:" + ex.Message);
			}
		}

		[AllowAnonymous]
		[HttpPost]
		public ActionResult Callback(Guid callbackId, DateTimeOffset callbackTime, DateTimeOffset currentServiceTime, string id)
		{
			Interlocked.Increment(ref _TotalCallbackCount);

			try
			{
				if (!RevaleeRegistrar.ValidateCallback(this.Request))
				{
					return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
			}

			Task.Run(() => _Log.Update(callbackId, currentServiceTime, this.Request.Url, id));
			return this.Content("OK");
		}

		protected override void OnException(ExceptionContext filterContext)
		{
			if (filterContext != null && filterContext.Exception != null)
			{
				System.Diagnostics.Debug.WriteLine(filterContext.Exception.Message);
			}
			base.OnException(filterContext);
		}
	}
}