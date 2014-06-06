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
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Routing;

namespace Revalee.Client.Mvc
{
	/// <summary>
	/// Extends <see cref="T:System.Web.Mvc.Controller" /> to add callback request methods.
	/// </summary>
	public static class RevaleeControllerExtensions
	{
		#region Time-based callbacks

		/// <summary>
		/// Schedules a callback at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackUri">An absolute <see cref="T:System.Uri" /> that will be requested on the callback.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackUri" /> is null.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="callbackUri" /> is not an absolute <see cref="T:System.Uri" />.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackAtAsync(this Controller controller, Uri callbackUri, DateTimeOffset callbackTime)
		{
			return SchedulingAgent.RequestCallbackAsync(callbackUri, callbackTime);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, DateTimeOffset callbackTime)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, null);
			return CallbackAtAsync(controller, callbackUri, callbackTime);
		}

		/// <summary>
		/// Schedules a callback to a controller action at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, DateTimeOffset callbackTime)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, null);
			return CallbackAtAsync(controller, callbackUri, callbackTime);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, RouteValueDictionary routeValues, DateTimeOffset callbackTime)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}
			
			Uri callbackUri = BuildCallbackUri(controller, actionName, null, routeValues);
			return CallbackAtAsync(controller, callbackUri, callbackTime);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, object routeValues, DateTimeOffset callbackTime)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, new RouteValueDictionary(routeValues));
			return CallbackAtAsync(controller, callbackUri, callbackTime);
		}

		/// <summary>
		/// Schedules a callback to a controller action at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues, DateTimeOffset callbackTime)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, routeValues);
			return CallbackAtAsync(controller, callbackUri, callbackTime);
		}

		/// <summary>
		/// Schedules a callback to a controller action at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, object routeValues, DateTimeOffset callbackTime)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, new RouteValueDictionary(routeValues));
			return CallbackAtAsync(controller, callbackUri, callbackTime);
		}

		#endregion Time-based callbacks

		#region Time-based callbacks with cancellation token

		/// <summary>
		/// Schedules a callback at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackUri">An absolute <see cref="T:System.Uri" /> that will be requested on the callback.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackUri" /> is null.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="callbackUri" /> is not an absolute <see cref="T:System.Uri" />.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackAtAsync(this Controller controller, Uri callbackUri, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
			return SchedulingAgent.RequestCallbackAsync(callbackUri, callbackTime, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, null);
			return CallbackAtAsync(controller, callbackUri, callbackTime, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to a controller action at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, null);
			return CallbackAtAsync(controller, callbackUri, callbackTime, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, RouteValueDictionary routeValues, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, routeValues);
			return CallbackAtAsync(controller, callbackUri, callbackTime, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, object routeValues, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, new RouteValueDictionary(routeValues));
			return CallbackAtAsync(controller, callbackUri, callbackTime, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to a controller action at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, routeValues);
			return CallbackAtAsync(controller, callbackUri, callbackTime, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to a controller action at a specified time.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackTime">A <see cref="T:System.DateTimeOffset" /> that represents the date and time to issue the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, object routeValues, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, new RouteValueDictionary(routeValues));
			return CallbackAtAsync(controller, callbackUri, callbackTime, cancellationToken);
		}

		#endregion Time-based callbacks with cancellation token

		#region Delay-based callbacks

		/// <summary>
		/// Schedules a callback after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackUri">An absolute <see cref="T:System.Uri" /> that will be requested on the callback.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackUri" /> is null.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="callbackUri" /> is not an absolute <see cref="T:System.Uri" />.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackAfterAsync(this Controller controller, Uri callbackUri, TimeSpan callbackDelay)
		{
			return SchedulingAgent.RequestCallbackAsync(callbackUri, DateTimeOffset.Now.Add(callbackDelay));
		}

		/// <summary>
		/// Schedules a callback to an action on this controller after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, TimeSpan callbackDelay)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, null);
			return CallbackAfterAsync(controller, callbackUri, callbackDelay);
		}

		/// <summary>
		/// Schedules a callback to a controller action after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, TimeSpan callbackDelay)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, null);
			return CallbackAfterAsync(controller, callbackUri, callbackDelay);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, RouteValueDictionary routeValues, TimeSpan callbackDelay)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, routeValues);
			return CallbackAfterAsync(controller, callbackUri, callbackDelay);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, object routeValues, TimeSpan callbackDelay)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, new RouteValueDictionary(routeValues));
			return CallbackAfterAsync(controller, callbackUri, callbackDelay);
		}

		/// <summary>
		/// Schedules a callback to a controller action after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues, TimeSpan callbackDelay)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, routeValues);
			return CallbackAfterAsync(controller, callbackUri, callbackDelay);
		}

		/// <summary>
		/// Schedules a callback to a controller action after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, object routeValues, TimeSpan callbackDelay)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, new RouteValueDictionary(routeValues));
			return CallbackAfterAsync(controller, callbackUri, callbackDelay);
		}

		#endregion Delay-based callbacks

		#region Delay-based callbacks with cancellation token

		/// <summary>
		/// Schedules a callback after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackUri">An absolute <see cref="T:System.Uri" /> that will be requested on the callback.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackUri" /> is null.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="callbackUri" /> is not an absolute <see cref="T:System.Uri" />.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackAfterAsync(this Controller controller, Uri callbackUri, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
			return SchedulingAgent.RequestCallbackAsync(callbackUri, DateTimeOffset.Now.Add(callbackDelay), cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, null);
			return CallbackAfterAsync(controller, callbackUri, callbackDelay, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to a controller action after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, null);
			return CallbackAfterAsync(controller, callbackUri, callbackDelay, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, RouteValueDictionary routeValues, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, routeValues);
			return CallbackAfterAsync(controller, callbackUri, callbackDelay, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, object routeValues, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, new RouteValueDictionary(routeValues));
			return CallbackAfterAsync(controller, callbackUri, callbackDelay, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to a controller action after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, routeValues);
			return CallbackAfterAsync(controller, callbackUri, callbackDelay, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to a controller action after a specified delay.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="callbackDelay">A <see cref="T:System.TimeSpan" /> that represents a time interval to delay the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, object routeValues, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, new RouteValueDictionary(routeValues));
			return CallbackAfterAsync(controller, callbackUri, callbackDelay, cancellationToken);
		}

		#endregion Delay-based callbacks with cancellation token

		#region Immediate callbacks

		/// <summary>
		/// Schedules an immediate callback.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackUri">An absolute <see cref="T:System.Uri" /> that will be requested on the callback.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackUri" /> is null.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="callbackUri" /> is not an absolute <see cref="T:System.Uri" />.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static async Task<Guid> CallbackNowAsync(this Controller controller, Uri callbackUri, object state)
		{
			Guid callbackId = await SchedulingAgent.RequestCallbackAsync(callbackUri, DateTimeOffset.MinValue);
			CallbackStateCache.StoreCallbackState(controller.HttpContext, callbackId, state, DateTime.Now.AddMinutes(10.0));
			return callbackId;
		}

		/// <summary>
		/// Schedules a callback to an action on this controller immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, object state)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, null);
			return CallbackNowAsync(controller, callbackUri, state);
		}

		/// <summary>
		/// Schedules a callback to a controller action immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, string controllerName, object state)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, null);
			return CallbackNowAsync(controller, callbackUri, state);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, RouteValueDictionary routeValues, object state)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, routeValues);
			return CallbackNowAsync(controller, callbackUri, state);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, object routeValues, object state)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, new RouteValueDictionary(routeValues));
			return CallbackNowAsync(controller, callbackUri, state);
		}

		/// <summary>
		/// Schedules a callback to a controller action immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues, object state)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, routeValues);
			return CallbackNowAsync(controller, callbackUri, state);
		}

		/// <summary>
		/// Schedules a callback to a controller action immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, string controllerName, object routeValues, object state)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, new RouteValueDictionary(routeValues));
			return CallbackNowAsync(controller, callbackUri, state);
		}

		#endregion Immediate callbacks

		#region Immediate callbacks with cancellation token

		/// <summary>
		/// Schedules an immediate callback.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackUri">An absolute <see cref="T:System.Uri" /> that will be requested on the callback.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackUri" /> is null.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="callbackUri" /> is not an absolute <see cref="T:System.Uri" />.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static async Task<Guid> CallbackNowAsync(this Controller controller, Uri callbackUri, object state, CancellationToken cancellationToken)
		{
			Guid callbackId = await SchedulingAgent.RequestCallbackAsync(callbackUri, DateTimeOffset.MinValue, cancellationToken);
			CallbackStateCache.StoreCallbackState(controller.HttpContext, callbackId, state, DateTime.Now.AddMinutes(10.0));
			return callbackId;
		}

		/// <summary>
		/// Schedules a callback to an action on this controller immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, object state, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, null);
			return CallbackNowAsync(controller, callbackUri, state, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to a controller action immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, string controllerName, object state, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, null);
			return CallbackNowAsync(controller, callbackUri, state, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, RouteValueDictionary routeValues, object state, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, routeValues);
			return CallbackNowAsync(controller, callbackUri, state, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to an action on this controller immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, object routeValues, object state, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, new RouteValueDictionary(routeValues));
			return CallbackNowAsync(controller, callbackUri, state, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to a controller action immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues, object state, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, routeValues);
			return CallbackNowAsync(controller, callbackUri, state, cancellationToken);
		}

		/// <summary>
		/// Schedules a callback to a controller action immediately.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="state">An <see cref="T:System.Object" /> containing the data to be used upon receiving the callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Guid" /> that serves as the identifier for the successfully scheduled callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The callback request failed during communication with the Revalee service.</exception>
		public static Task<Guid> CallbackToActionNowAsync(this Controller controller, string actionName, string controllerName, object routeValues, object state, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, new RouteValueDictionary(routeValues));
			return CallbackNowAsync(controller, callbackUri, state, cancellationToken);
		}

		#endregion Immediate callbacks with cancellation token

		#region Callback cancellation

		/// <summary>
		/// Cancels a previously scheduled callback.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="callbackUri">The requested absolute <see cref="T:System.Uri" /> of the previously scheduled callback.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackUri" /> is null.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="callbackUri" /> is not an absolute <see cref="T:System.Uri" />.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static async Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, Uri callbackUri)
		{
			bool success = await SchedulingAgent.CancelCallbackAsync(callbackId, callbackUri);

			if (success)
			{
				CallbackStateCache.DeleteCallbackState(controller.HttpContext, callbackId);
			}

			return success;
		}

		/// <summary>
		/// Cancels a previously scheduled callback to an action on this controller.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, null);
			return CancelCallbackAsync(controller, callbackId, callbackUri);
		}

		/// <summary>
		/// Cancels a previously scheduled callback to a controller action.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName, string controllerName)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, null);
			return CancelCallbackAsync(controller, callbackId, callbackUri);
		}

		/// <summary>
		/// Cancels a previously scheduled callback to an action on this controller.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName, RouteValueDictionary routeValues)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, routeValues);
			return CancelCallbackAsync(controller, callbackId, callbackUri);
		}

		/// <summary>
		/// Cancels a previously scheduled callback to an action on this controller.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName, object routeValues)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, new RouteValueDictionary(routeValues));
			return CancelCallbackAsync(controller, callbackId, callbackUri);
		}

		/// <summary>
		/// Cancels a previously scheduled callback to a controller action.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName, string controllerName, RouteValueDictionary routeValues)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, routeValues);
			return CancelCallbackAsync(controller, callbackId, callbackUri);
		}

		/// <summary>
		/// Cancels a previously scheduled callback to a controller action.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName, string controllerName, object routeValues)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, new RouteValueDictionary(routeValues));
			return CancelCallbackAsync(controller, callbackId, callbackUri);
		}

		#endregion Callback cancellation

		#region Callback cancellation with cancellation token

		/// <summary>
		/// Cancels a previously scheduled callback.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="callbackUri">The requested absolute <see cref="T:System.Uri" /> of the previously scheduled callback.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackUri" /> is null.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="callbackUri" /> is not an absolute <see cref="T:System.Uri" />.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static async Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, Uri callbackUri, CancellationToken cancellationToken)
		{
			bool success = await SchedulingAgent.CancelCallbackAsync(callbackId, callbackUri, cancellationToken);

			if (!cancellationToken.IsCancellationRequested && success)
			{
				CallbackStateCache.DeleteCallbackState(controller.HttpContext, callbackId);
			}

			return success;
		}

		/// <summary>
		/// Cancels a previously scheduled callback to an action on this controller.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, null);
			return CancelCallbackAsync(controller, callbackId, callbackUri, cancellationToken);
		}

		/// <summary>
		/// Cancels a previously scheduled callback to a controller action.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName, string controllerName, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, null);
			return CancelCallbackAsync(controller, callbackId, callbackUri, cancellationToken);
		}

		/// <summary>
		/// Cancels a previously scheduled callback to an action on this controller.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName, RouteValueDictionary routeValues, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, routeValues);
			return CancelCallbackAsync(controller, callbackId, callbackUri, cancellationToken);
		}

		/// <summary>
		/// Cancels a previously scheduled callback to an action on this controller.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName, object routeValues, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, null, new RouteValueDictionary(routeValues));
			return CancelCallbackAsync(controller, callbackId, callbackUri, cancellationToken);
		}

		/// <summary>
		/// Cancels a previously scheduled callback to a controller action.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName, string controllerName, RouteValueDictionary routeValues, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, routeValues);
			return CancelCallbackAsync(controller, callbackId, callbackUri, cancellationToken);
		}

		/// <summary>
		/// Cancels a previously scheduled callback to a controller action.
		/// </summary>
		/// <param name="controller">The <see cref="T:System.Web.Mvc.Controller" /> instance that this method extends.</param>
		/// <param name="callbackId">The <see cref="T:System.Guid" /> that identifies the previously scheduled callback.</param>
		/// <param name="actionName">The name of the action.</param>
		/// <param name="controllerName">The name of the controller.</param>
		/// <param name="routeValues">The parameters for a route.</param>
		/// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that controls the cancellation of the operation.</param>
		/// <returns>A task that represents the asynchronous operation.
		/// The task result contains the <see cref="T:System.Boolean" /> that will indicate true if the cancellation was processed normally.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackId" /> is an empty <see cref="T:System.Guid" />.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="actionName" /> is null or empty.</exception>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="controllerName" /> is null or empty.</exception>
		/// <exception cref="T:Revalee.Client.RevaleeRequestException">The cancellation request failed during communication with the Revalee service.</exception>
		public static Task<bool> CancelCallbackAsync(this Controller controller, Guid callbackId, string actionName, string controllerName, object routeValues, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(actionName))
			{
				throw new ArgumentNullException("actionName");
			}

			if (string.IsNullOrEmpty(controllerName))
			{
				throw new ArgumentNullException("controllerName");
			}

			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, new RouteValueDictionary(routeValues));
			return CancelCallbackAsync(controller, callbackId, callbackUri, cancellationToken);
		}

		#endregion Callback cancellation with cancellation token

		#region Uri construction

		private static Uri BuildCallbackUri(Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues)
		{
			string callbackUrlLeftPart = controller.Request.Url.GetLeftPart(UriPartial.Authority);
			RouteValueDictionary mergedRouteValues = MergeRouteValues(controller.RouteData.Values, actionName, controllerName, routeValues);
			string callbackUrlRightPart = UrlHelper.GenerateUrl(null, null, null, null, null, null, mergedRouteValues, RouteTable.Routes, controller.Request.RequestContext, false);
			return new Uri(new Uri(callbackUrlLeftPart, UriKind.Absolute), callbackUrlRightPart);
		}

		private static RouteValueDictionary MergeRouteValues(RouteValueDictionary currentRouteValues, string actionName, string controllerName, RouteValueDictionary routeValues)
		{
			if (routeValues == null)
			{
				routeValues = new RouteValueDictionary();
			}

			if (actionName == null)
			{
				object actionValue;
				if (currentRouteValues != null && currentRouteValues.TryGetValue("action", out actionValue))
				{
					routeValues["action"] = actionValue;
				}
			}
			else
			{
				routeValues["action"] = actionName;
			}

			if (controllerName == null)
			{
				object controllerValue;
				if (currentRouteValues != null && currentRouteValues.TryGetValue("controller", out controllerValue))
				{
					routeValues["controller"] = controllerValue;
				}
			}
			else
			{
				routeValues["controller"] = controllerName;
			}

			return routeValues;
		}

		#endregion Uri construction
	}
}