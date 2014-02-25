using System;
using System.Collections.Generic;
using System.Threading;

namespace Revalee.Service
{
	public class UrlMatchDictionary<T> : IPartialMatchDictionary<Uri, T> where T : class
	{
		private readonly object _SyncRoot = new object();
		private readonly Dictionary<string, T> _UrlList = new Dictionary<string, T>();
		private IndexedCollection _UrlIndexedCollection = null;
		private bool _ReindexingRequired;

		private class IndexedCollection
		{
			public IndexedCollection(string[] matchKeys, T[] payloads)
			{
				this.MatchKeys = matchKeys;
				this.Payloads = payloads;
			}

			public readonly string[] MatchKeys;
			public readonly T[] Payloads;
		}

		public UrlMatchDictionary()
		{
		}

		public void Add(Uri urlPrefix, T payload)
		{
			if (urlPrefix == null)
			{
				throw new ArgumentNullException("urlPrefix");
			}

			if (payload == null)
			{
				throw new ArgumentNullException("payload");
			}

			string matchKey = BuildMatchKey(urlPrefix);

			lock (_SyncRoot)
			{
				try
				{
					_UrlList.Add(matchKey, payload);
					ScheduleIndexing();
				}
				catch (ArgumentException)
				{
					throw new ArgumentException("Duplicate url prefix found.", "urlPrefix");
				}
			}
		}

		public T Match(Uri url)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}

			string matchKey = BuildMatchKey(url);
			if (_UrlIndexedCollection != null)
			{
				return IndexedMatch(matchKey);
			}
			else
			{
				return ScannedMatch(matchKey);
			}
		}

		private T IndexedMatch(string matchKey)
		{
			IndexedCollection urlIndex = _UrlIndexedCollection;

			if (urlIndex.MatchKeys.Length == 0)
			{
				return default(T);
			}

			int matchIndex = Array.BinarySearch<string>(urlIndex.MatchKeys, matchKey);

			if (matchIndex < 0)
			{
				// If the index is negative, it represents the bitwise
				// complement of the next larger element in the array.

				// The element before the larger element is a candidate of a partial match
				int candidateMatchIndex = (matchIndex ^ -1) - 1;

				if (candidateMatchIndex < 0)
				{
					return default(T);
				}

				if (matchKey.StartsWith(urlIndex.MatchKeys[candidateMatchIndex], StringComparison.Ordinal))
				{
					return urlIndex.Payloads[candidateMatchIndex];
				}

				return default(T);
			}
			else
			{
				return urlIndex.Payloads[matchIndex];
			}
		}

		private T ScannedMatch(string matchKey)
		{
			lock (_SyncRoot)
			{
				T exactMatchPayload = default(T);
				if (_UrlList.TryGetValue(matchKey, out exactMatchPayload))
				{
					return exactMatchPayload;
				}

				int bestMatchCount = 0;
				string bestMatchKey = null;
				int matchMaxIndex = matchKey.Length - 1;

				foreach (string key in _UrlList.Keys)
				{
					int matchCount = 0;
					for (int charIndex = 0; charIndex < key.Length; charIndex++)
					{
						if (charIndex > matchMaxIndex)
						{
							matchCount = 0;
							break;
						}
						else if (matchKey[charIndex] == key[charIndex])
						{
							matchCount += 1;
						}
						else
						{
							matchCount = 0;
							break;
						}
					}

					if (matchCount > bestMatchCount)
					{
						bestMatchCount = matchCount;
						bestMatchKey = key;
						if (matchCount == matchMaxIndex)
						{
							// The best possible match is found
							break;
						}
					}
				}

				if (bestMatchKey == null)
				{
					return default(T);
				}

				return _UrlList[bestMatchKey];
			}
		}

		private static string BuildMatchKey(Uri url)
		{
			return url.OriginalString;
		}

		private void ScheduleIndexing()
		{
			if (_ReindexingRequired)
			{
				return;
			}

			_ReindexingRequired = true;
			ThreadPool.QueueUserWorkItem(new WaitCallback(BuildIndex));
		}

		private void BuildIndex(object state)
		{
			if (!_ReindexingRequired)
			{
				return;
			}

			Thread.Sleep(TimeSpan.FromSeconds(1.0));

			if (!_ReindexingRequired)
			{
				return;
			}

			string[] matchKeys;
			T[] payloads;

			lock (_SyncRoot)
			{
				if (!_ReindexingRequired)
				{
					return;
				}

				_ReindexingRequired = false;
				matchKeys = new string[_UrlList.Count];
				payloads = new T[_UrlList.Count];
				_UrlList.Keys.CopyTo(matchKeys, 0);
				_UrlList.Values.CopyTo(payloads, 0);
			}

			Array.Sort(matchKeys, payloads);
			_UrlIndexedCollection = new IndexedCollection(matchKeys, payloads);
		}
	}
}