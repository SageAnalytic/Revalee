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
using System.Linq;
using System.Web.Mvc;

namespace Revalee.Client.Mvc
{
	/// <summary>
	/// Represents an attribute that will restrict access to previously requested callbacks.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public sealed class CallbackActionAttribute : ActionFilterAttribute, IAuthorizationFilter
	{
		/// <summary>
		/// Called when a process requests authorization for the marked callback action.
		/// </summary>
		/// <param name="filterContext">The filter context, which encapsulates information for using <see cref="T:Revalee.Client.Mvc.CallbackActionAttribute" />.</param>
		/// <exception cref="T:System.ArgumentNullException">The <paramref name="filterContext" /> parameter is null.</exception>
		public void OnAuthorization(AuthorizationContext filterContext)
		{
			if (filterContext == null)
			{
				throw new ArgumentNullException("filterContext");
			}

			if (filterContext.HttpContext == null
				|| filterContext.HttpContext.Request == null
				|| !RevaleeRegistrar.ValidateCallback(filterContext.HttpContext.Request))
			{
				filterContext.Result = new HttpUnauthorizedResult();
			}
		}

		/// <summary>
		/// Called before the action executes to supply cached state information if present.
		/// </summary>
		/// <param name="filterContext">The filter context, which encapsulates information for using <see cref="T:Revalee.Client.Mvc.CallbackActionAttribute" />.</param>
		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);

			if (filterContext == null)
			{
				throw new ArgumentNullException("filterContext");
			}

			const string callbackIdFormParameterName = "callbackId";
			const string stateActionParameterName = "state";

			if (filterContext.HttpContext != null
				&& filterContext.HttpContext.Request != null
				&& filterContext.ActionParameters != null
				&& filterContext.ActionDescriptor != null
				)
			{
				string callbackId = filterContext.HttpContext.Request.Form[callbackIdFormParameterName];

				if (!string.IsNullOrEmpty(callbackId))
				{
					if (filterContext.ActionParameters.ContainsKey(stateActionParameterName))
					{
						object cachedState = CallbackStateCache.RecoverCallbackState(filterContext.HttpContext, callbackId);

						if (cachedState != null)
						{
							ParameterDescriptor stateParameter = filterContext.ActionDescriptor.GetParameters().Where(p => string.Equals(p.ParameterName, stateActionParameterName, StringComparison.OrdinalIgnoreCase)).SingleOrDefault();

							if (stateParameter != null && stateParameter.ParameterType.IsAssignableFrom(cachedState.GetType()))
							{
								filterContext.ActionParameters[stateActionParameterName] = cachedState;
							}
						}
					}
				}
			}
		}
	}
}