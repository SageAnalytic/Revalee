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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, DateTimeOffset callbackTime)
		{
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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, DateTimeOffset callbackTime)
		{
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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, RouteValueDictionary routeValues, DateTimeOffset callbackTime)
		{
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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, object routeValues, DateTimeOffset callbackTime)
		{
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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues, DateTimeOffset callbackTime)
		{
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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, object routeValues, DateTimeOffset callbackTime)
		{
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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, RouteValueDictionary routeValues, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, object routeValues, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
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
		public static Task<Guid> CallbackToActionAtAsync(this Controller controller, string actionName, string controllerName, object routeValues, DateTimeOffset callbackTime, CancellationToken cancellationToken)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, TimeSpan callbackDelay)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, TimeSpan callbackDelay)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, RouteValueDictionary routeValues, TimeSpan callbackDelay)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, object routeValues, TimeSpan callbackDelay)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues, TimeSpan callbackDelay)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, object routeValues, TimeSpan callbackDelay)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, RouteValueDictionary routeValues, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, object routeValues, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, RouteValueDictionary routeValues, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
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
		public static Task<Guid> CallbackToActionAfterAsync(this Controller controller, string actionName, string controllerName, object routeValues, TimeSpan callbackDelay, CancellationToken cancellationToken)
		{
			Uri callbackUri = BuildCallbackUri(controller, actionName, controllerName, new RouteValueDictionary(routeValues));
			return CallbackAfterAsync(controller, callbackUri, callbackDelay, cancellationToken);
		}

		#endregion Delay-based callbacks with cancellation token

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