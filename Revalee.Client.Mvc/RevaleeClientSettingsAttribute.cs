#region License

/*
The MIT License (MIT)

Copyright (c) 2014 Sage Analytic Technologies, LLC

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#endregion License

using System;
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
				if (_ServiceBaseUri == null)
				{
					return null;
				}

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

		/// <summary>Gets or sets the timeout of callback requests in milliseconds, a value of 0 indicates a default timeout period, a value of -1 indicates an infinite timeout period.</summary>
		/// <returns>The timeout of callback requests in milliseconds, a value of 0 indicates a default timeout period, a value of -1 indicates an infinite timeout period.</returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="value" /> cannot be a negative value unless -1 for an infinite period.</exception>
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
					return 0;
				}
			}
			set
			{
				if (value < -1)
				{
					throw new ArgumentOutOfRangeException("value");
				}

				if (value == 0)
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
		/// <exception cref="T:System.ArgumentNullException">The <paramref name="filterContext" /> parameter is null.</exception>
		public void OnActionExecuting(ActionExecutingContext filterContext)
		{
			if (filterContext == null)
			{
				throw new ArgumentNullException("filterContext");
			}

			if (_RequestTimeout.HasValue)
			{
				RevaleeClientSettings.RequestTimeout = _RequestTimeout;
			}

			if (_ServiceBaseUri != null)
			{
				RevaleeClientSettings.ServiceBaseUri = _ServiceBaseUri;
			}
		}
	}
}