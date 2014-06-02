using System;
using System.Web;

namespace Revalee.Client.Mvc
{
	internal static class CallbackStateCache
	{
		internal static void StoreCallbackState(HttpContextBase context, Guid callbackId, object state, DateTime expirationTime)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			if (Guid.Empty.Equals(callbackId))
			{
				throw new ArgumentNullException("callbackId");
			}

			if (context.Cache == null)
			{
				return;
			}

			if (state == null)
			{
				RemoveFromCache(context, callbackId.ToString());
				return;
			}

			AddToCache(context, callbackId.ToString(), state, expirationTime);
		}

		internal static void StoreCallbackState(HttpContextBase context, Guid callbackId, object state, TimeSpan expirationTimeSpan)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			if (Guid.Empty.Equals(callbackId))
			{
				throw new ArgumentNullException("callbackId");
			}

			if (context.Cache == null)
			{
				return;
			}

			if (state == null)
			{
				RemoveFromCache(context, callbackId.ToString());
				return;
			}

			AddToCache(context, callbackId.ToString(), state, DateTime.Now.Add(expirationTimeSpan));
		}

		internal static void DeleteCallbackState(HttpContextBase context, Guid callbackId)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			if (context.Cache == null)
			{
				return;
			}

			RemoveFromCache(context, callbackId.ToString());
		}

		internal static object RecoverCallbackState(HttpContextBase context, Guid callbackId)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			if (context.Cache == null)
			{
				return null;
			}

			return RemoveFromCache(context, callbackId.ToString());
		}

		internal static object RecoverCallbackState(HttpContextBase context, string cacheKey)
		{
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}

			if (cacheKey == null)
			{
				throw new ArgumentNullException("cacheKey");
			}

			if (context.Cache == null)
			{
				return null;
			}

			return RemoveFromCache(context, cacheKey);
		}

		private static void AddToCache(HttpContextBase context, string cacheKey, object state, DateTime expirationTime)
		{
			context.Cache.Insert(cacheKey, state, null, expirationTime, System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.High, null);
		}

		private static object RemoveFromCache(HttpContextBase context, string cacheKey)
		{
			return context.Cache.Remove(cacheKey);
		}
	}
}