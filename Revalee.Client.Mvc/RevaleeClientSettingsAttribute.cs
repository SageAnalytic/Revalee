using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Revalee.Client.Mvc
{
	/// <summary>Represents an attribute that is used to configure callback requests made during this request.</summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
	public sealed class RevaleeClientSettingsAttribute : FilterAttribute, IActionFilter 
	{
		private readonly object _TypeId = new object();
		private Uri _ServiceBaseUri;
		private int? _RequestTimeout;


		/// <summary>Gets or sets the service base URL used to make callback requests.</summary>
		/// <returns>The service base URL used to make callback requests.</returns>
		public string ServiceBaseUri
		{
			get
			{
				return _ServiceBaseUri.ToString();
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					_ServiceBaseUri = null;
				}
				else
				{
					_ServiceBaseUri = new ServiceBaseUri(value);
				}
			}
		}


		/// <summary>Gets or sets the timeout of callback requests in milliseconds, a value of -1 indicates a default timeout period.</summary>
		/// <returns>The timeout of callback requests in milliseconds, a value of -1 indicates a default timeout period.</returns>
		public int RequestTimeout
		{
			get
			{
				if (_RequestTimeout.HasValue)
				{
					return _RequestTimeout.Value;
				}
				else
				{
					return -1;
				}
			}
			set
			{
				if (value < -1)
				{
					throw new ArgumentOutOfRangeException("RequestTimeout");
				}
				else if (value == -1)
				{
					_RequestTimeout = null;
				}
				else
				{
					_RequestTimeout = value;
				}
			}
		}

		/// <summary>Gets the unique identifier for this attribute.</summary>
		/// <returns>The unique identifier for this attribute.</returns>
		public override object TypeId
		{
			get
			{
				return this._TypeId;
			}
		}

		/// <summary>Called by the ASP.NET MVC framework after the action method executes.</summary>
		/// <param name="filterContext">The filter context.</param>
		public void OnActionExecuted(ActionExecutedContext filterContext)
		{
		}

		/// <summary>Called by the ASP.NET MVC framework before the action method executes.</summary>
		/// <param name="filterContext">The filter context.</param>
		public void OnActionExecuting(ActionExecutingContext filterContext)
		{
			if (filterContext == null)
			{
				throw new ArgumentNullException("filterContext");
			}

			if (_RequestTimeout.HasValue)
			{
				RevaleeClientSettings.RequestTimeout = _RequestTimeout.Value;
			}

			if (_ServiceBaseUri != null)
			{
				RevaleeClientSettings.ServiceBaseUri = _ServiceBaseUri;
			}
		}
	}
}
