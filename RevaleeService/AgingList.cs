using System;
using System.Collections;
using System.Collections.Generic;

namespace RevaleeService
{
	public class AgingList<TKey> : ICollection
	{
		private readonly WaitingList<TKey> _Expirations;
		private readonly KeyedList<TKey> _Keys;
		private readonly IEqualityComparer<TKey> _Comparer;

		public AgingList()
		{
			_Expirations = new WaitingList<TKey>();
			_Keys = new KeyedList<TKey>();
			_Comparer = EqualityComparer<TKey>.Default;
		}

		public AgingList(int capacity)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity");
			}

			_Expirations = new WaitingList<TKey>(capacity);
			_Keys = new KeyedList<TKey>(capacity);
			_Comparer = EqualityComparer<TKey>.Default;
		}

		public AgingList(IEqualityComparer<TKey> comparer)
		{
			_Expirations = new WaitingList<TKey>();
			_Keys = new KeyedList<TKey>();

			if (comparer == null)
			{
				_Comparer = EqualityComparer<TKey>.Default;
			}
			else
			{
				_Comparer = comparer;
			}
		}

		public AgingList(int capacity, IEqualityComparer<TKey> comparer)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity");
			}

			_Expirations = new WaitingList<TKey>(capacity);
			_Keys = new KeyedList<TKey>(capacity);

			if (comparer == null)
			{
				_Comparer = EqualityComparer<TKey>.Default;
			}
			else
			{
				_Comparer = comparer;
			}
		}

		public bool Contains(TKey key)
		{
			if (PruneExpired(key))
			{
				return false;
			}

			return _Keys.Contains(key);
		}

		public void Add(TKey key, DateTimeOffset expiration)
		{
			if (PruneExpired(key))
			{
				_Keys.Add(key);
				_Expirations.Add(key, expiration);
			}
			else if (!_Keys.Contains(key))
			{
				_Keys.Add(key);
				_Expirations.Add(key, expiration);
			}
		}

		public int Count
		{
			get
			{
				PruneExpired();
				return _Keys.Count;
			}
		}

		public void Clear()
		{
			_Expirations.Clear();
			_Keys.Clear();
		}

		public void CopyTo(TKey[] array, int arrayIndex)
		{
			((ICollection)_Keys).CopyTo(array, arrayIndex);
		}

		public IEnumerator<TKey> GetEnumerator()
		{
			return _Keys.GetEnumerator();
		}

		private void PruneExpired()
		{
			while (this.ContainsOldEntries)
			{
				TKey key = _Expirations.Dequeue();
				_Keys.Remove(key);
			}
		}

		private bool PruneExpired(TKey key)
		{
			bool keyRemoved = false;

			while (this.ContainsOldEntries)
			{
				TKey expiredKey = _Expirations.Dequeue();
				_Keys.Remove(expiredKey);

				if (_Comparer.Equals(key, expiredKey))
				{
					keyRemoved = true;
				}
			}

			return keyRemoved;
		}

		private bool ContainsOldEntries
		{
			get
			{
				DateTimeOffset? oldestEntryTime = _Expirations.PeekNextTime();
				if (oldestEntryTime.HasValue)
				{
					return (oldestEntryTime.Value < DateTimeOffset.Now.AddHours(-24.0));
				}
				else
				{
					return false;
				}
			}
		}

		void ICollection.CopyTo(Array array, int index)
		{
			((ICollection)_Keys).CopyTo(array, index);
		}

		bool ICollection.IsSynchronized
		{
			get
			{
				return ((ICollection)_Keys).IsSynchronized;
			}
		}

		object ICollection.SyncRoot
		{
			get
			{
				return ((ICollection)_Keys).SyncRoot;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((ICollection)_Keys).GetEnumerator();
		}
	}
}