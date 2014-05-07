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
	public class RecurringTaskModule : IHttpModule
	{
		private const string _ManifestApplicationKey = "RevaleeLifecycleSessionIdentifier";
		private const string _InProcessContextKey = "RevaleeRecurringTask";

		private static TaskManifest _Manifest = LoadManifest();

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

		public static ITaskManifest GetManifest()
		{
			return _Manifest;
		}

		public static CallbackDetails RecurringCallbackDetails
		{
			get
			{
				return (CallbackDetails)HttpContext.Current.Items[_InProcessContextKey];
			}
		}

		public static bool IsProcessingRecurringCallback
		{
			get
			{
				return HttpContext.Current.Items.Contains(_InProcessContextKey);
			}
		}

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

					if (_Manifest.TryGetTask(analysis.TaskIdentifier, out taskConfig))
					{
						if (RevaleeRegistrar.ValidateCallback(new HttpRequestWrapper(request)))
						{
							if (taskConfig.SetLastOccurrence(analysis.Occurrence))
							{
								application.Context.Items.Add(_InProcessContextKey, BuildCallbackDetails(request));
								application.Context.RewritePath(taskConfig.Url.AbsolutePath, true);
								_Manifest.Reschedule(taskConfig);
								return;
							}
						}
					}

					application.Context.Response.StatusCode = (int)HttpStatusCode.OK;
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

		private static CallbackDetails BuildCallbackDetails(HttpRequest request)
		{
			return new CallbackDetails(request.Form["CallbackId"], request.Form["CallbackTime"], request.Form["CurrentServiceTime"]);
		}
	}
}