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

using Revalee.Client.Configuration;
using System;
using System.Net;
using System.Web;

namespace Revalee.Client.RecurringTasks
{
	/// <summary>
	/// HttpModule that schedules and intercepts recurring tasks.
	/// </summary>
	public class RecurringTaskModule : IHttpModule
	{
		private const string _ManifestApplicationKey = "RevaleeLifecycleSessionIdentifier";
		private const string _InProcessContextKey = "RevaleeRecurringTask";

		private static TaskManifest _Manifest = LoadManifest();

		/// <summary>
		/// Initializes the recurring tasks module.
		/// </summary>
		/// <param name="context">The currently executing <see cref="T:System.Web.HttpApplication" /> instance.</param>
		public void Init(HttpApplication context)
		{
			if (context != null)
			{
				if (_Manifest != null)
				{
					context.BeginRequest += context_BeginRequest;
				}
			}
		}

		/// <summary>
		/// Reactivates the <see cref="T:Revalee.Client.RecurringTasks.RecurringTaskModule" /> following an unintended deactivation.
		/// </summary>
		public void Restart()
		{
			if (_Manifest != null)
			{
				if (!_Manifest.IsEmpty)
				{
					if (!_Manifest.IsActive)
					{
						_Manifest.Start();
					}
				}
			}
		}

		/// <summary>
		/// Retrieves the current <see cref="T:Revalee.Client.RecurringTasks.ITaskManifest" /> instance that manages recurring tasks.
		/// </summary>
		/// <returns>An <see cref="T:Revalee.Client.RecurringTasks.ITaskManifest" /> instance for the current <see cref="T:System.Web.HttpApplication" />.</returns>
		public static ITaskManifest GetManifest()
		{
			return _Manifest;
		}

		/// <summary>
		/// Gets a <see cref="T:Revalee.Client.RecurringTasks.RecurringTaskCallbackDetails" /> instance if the the current request is the result of a recurring callback.
		/// </summary>
		public static RecurringTaskCallbackDetails CallbackDetails
		{
			get
			{
				return (RecurringTaskCallbackDetails)HttpContext.Current.Items[_InProcessContextKey];
			}
		}

		/// <summary>
		/// Gets a <see cref="T:System.Boolean" /> value indicating that the current request is the result of a recurring callback.
		/// </summary>
		public static bool IsProcessingRecurringCallback
		{
			get
			{
				return HttpContext.Current.Items.Contains(_InProcessContextKey);
			}
		}

		/// <summary>
		/// Disposes the module.
		/// </summary>
		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		private void context_BeginRequest(object sender, EventArgs e)
		{
			HttpApplication application = sender as HttpApplication;

			if (application != null && application.Context != null && application.Request != null && _Manifest != null)
			{
				HttpRequest request = application.Request;

				RequestAnalysis analysis = _Manifest.AnalyzeRequest(request);

				if (analysis.IsRecurringTask)
				{
					ConfiguredTask taskConfig;
					HttpStatusCode statusCode;

					if (_Manifest.TryGetTask(analysis.TaskIdentifier, out taskConfig))
					{
						if (request.HttpMethod == "POST")
						{
							if (RevaleeRegistrar.ValidateCallback(new HttpRequestWrapper(request)))
							{
								if (taskConfig.SetLastOccurrence(analysis.Occurrence))
								{
									_Manifest.Reschedule(taskConfig);
									application.Context.Items.Add(_InProcessContextKey, BuildCallbackDetails(request));
									application.Context.RewritePath(taskConfig.Url.AbsolutePath, true);
									return;
								}
								else
								{
									statusCode = HttpStatusCode.Accepted;
								}
							}
							else
							{
								statusCode = HttpStatusCode.Unauthorized;
							}
						}
						else
						{
							if (request.HttpMethod == "GET" || request.HttpMethod == "HEAD")
							{
								if (request.Headers["Expect"] == "100-continue")
								{
									application.Context.Response.StatusCode = (int)HttpStatusCode.Continue;
									return;
								}
								else
								{
									statusCode = HttpStatusCode.MethodNotAllowed;
								}
							}
							else
							{
								statusCode = HttpStatusCode.NotImplemented;
							}
						}
					}
					else
					{
						statusCode = HttpStatusCode.NoContent;
					}

					application.Context.Response.StatusCode = (int)statusCode;
					application.Context.Response.SuppressContent = true;
					application.CompleteRequest();
					return;
				}
			}
		}

		private static TaskManifest LoadManifest()
		{
			TaskManifest manifest = null;

			RevaleeSection configSection = RevaleeSection.GetConfiguration();

			if (configSection == null)
			{
				manifest = new TaskManifest();
			}
			else
			{
				manifest = new TaskManifest(configSection.RecurringTasks);

				if (!manifest.IsEmpty)
				{
					manifest.Start();
				}
			}

			return manifest;
		}

		private static RecurringTaskCallbackDetails BuildCallbackDetails(HttpRequest request)
		{
			return new RecurringTaskCallbackDetails(request.Form["CallbackId"], request.Form["CallbackTime"], request.Form["CurrentServiceTime"]);
		}
	}
}