using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace Revalee.Service
{
	[ComVisibleAttribute(false)]
	[HostProtectionAttribute(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	public class ScheduledDictionary<TKey, TValue> : ICollection
	{
		#region Constants

		private const int _MinimumCapacity = 17;

		private readonly static int[] _Primes = new int[] {
			3, 7, 11, 0x11, 0x17, 0x1d, 0x25, 0x2f, 0x3b, 0x47, 0x59, 0x6b, 0x83, 0xa3,
			0xc5, 0xef, 0x125, 0x161, 0x1af, 0x209, 0x277, 0x2f9, 0x397, 0x44f, 0x52f, 0x63d, 0x78b,
			0x91d, 0xaf1, 0xd2b, 0xfd1, 0x12fd, 0x16cf, 0x1b65, 0x20e3, 0x2777, 0x2f6f, 0x38ff, 0x446f,
			0x521f, 0x628d, 0x7655, 0x8e01, 0xaa6b, 0xcc89, 0xf583, 0x126a7, 0x1619b, 0x1a857, 0x1fd3b,
			0x26315, 0x2dd67, 0x3701b, 0x42023, 0x4f361, 0x5f0ed, 0x72125, 0x88e31, 0xa443b, 0xc51eb,
			0xec8c1, 0x11bdbf, 0x154a3f, 0x198c4f, 0x1ea867, 0x24ca19, 0x2c25c1, 0x34fa1b, 0x3f928f,
			0x4c4987, 0x5b8b6f, 0x6dda89};

		#endregion Constants

		#region Variables

		private HeapEntry[] _HeapEntries;
		private int[] _HashtableBuckets;
		private HashtableEntry[] _HashtableEntries;
		private int _HashtableNextFreeBucketIndex;
		private int _HashtableFreeBucketCount;
		private int _HashtableSize;
		private long _Version = long.MinValue;
		private object _SyncRoot;
		private bool _IsHeapOrdered;
		private IEqualityComparer<TKey> _Comparer;
		private int _PendingDeleteCount;
		private Action _StateTransitionAction = null;
		private KeyEnumerable _KeyEnumerable;
		private ValueEnumerable _ValueEnumerable;
		private OverdueValueEnumerable _OverdueValueEnumerable;

		private readonly int _InitialCapacity;
		private readonly object _InternalLock = new object();

		#endregion Variables

		#region Public Members

		public ScheduledDictionary()
			: this(_MinimumCapacity, null)
		{
		}

		public ScheduledDictionary(int capacity)
			: this(capacity, null)
		{
		}

		public ScheduledDictionary(IEqualityComparer<TKey> comparer)
			: this(_MinimumCapacity, comparer)
		{
		}

		public ScheduledDictionary(int capacity, IEqualityComparer<TKey> comparer)
		{
			if (capacity < 0)
			{
				throw new ArgumentOutOfRangeException("capacity");
			}

			if (capacity <= _MinimumCapacity)
			{
				_InitialCapacity = _MinimumCapacity;
			}
			else
			{
				_InitialCapacity = GetPrime(capacity + 1);
			}

			InitializeHeap(_InitialCapacity);
			InitializeHashtable(_InitialCapacity);

			if (comparer == null)
			{
				_Comparer = EqualityComparer<TKey>.Default;
			}
			else
			{
				_Comparer = comparer;
			}
		}

		public bool TryAdd(TKey key, TValue value, DateTime due)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			lock (_InternalLock)	// Write operation
			{
				int heapIndex = FindHeapIndexInHashtable(key);

				if (heapIndex >= 0)
				{
					return false;
				}

				_Version++;
				heapIndex = InsertIntoHeap(key, value, due);
				TrackModification();
			}

			return true;
		}

		public void AddOrReplace(TKey key, TValue value, DateTime due)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			lock (_InternalLock)	// Write operation
			{
				int heapIndex = FindHeapIndexInHashtable(key);

				_Version++;

				if (heapIndex >= 0)
				{
					HeapEntry cancelledAppointment = _HeapEntries[heapIndex];
					cancelledAppointment.IsDeleted = true;
					DeleteFromHashtable(key);
					_PendingDeleteCount++;
				}

				heapIndex = InsertIntoHeap(key, value, due);
				TrackModification();
			}
		}

		public TValue this[TKey key]
		{
			get
			{
				if (key == null)
				{
					throw new ArgumentNullException("key");
				}

				lock (_InternalLock)	// Read operation
				{
					int heapIndex = FindHeapIndexInHashtable(key);

					if (heapIndex < 0)
					{
						throw new KeyNotFoundException();
					}

					return _HeapEntries[heapIndex].Value;
				}
			}
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			lock (_InternalLock)	// Read operation
			{
				int heapIndex = FindHeapIndexInHashtable(key);

				if (heapIndex < 0)
				{
					value = default(TValue);
					return false;
				}

				value = _HeapEntries[heapIndex].Value;
				return true;
			}
		}

		public bool TryRemove(TKey key, out TValue result)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			lock (_InternalLock)	// Write operation
			{
				int heapIndex = FindHeapIndexInHashtable(key);

				if (heapIndex < 0)
				{
					result = default(TValue);
					return false;
				}

				_Version++;
				HeapEntry cancelledAppointment = _HeapEntries[heapIndex];
				cancelledAppointment.IsDeleted = true;
				DeleteFromHashtable(key);
				_PendingDeleteCount++;
				TrackModification();

				result = cancelledAppointment.Value;
				return true;
			}
		}

		public int RemoveAllOverdue()
		{
			lock (_InternalLock)	// Write operation
			{
				int removedEntries = PruneOverdueEntries();

				if (removedEntries > 0)
				{
					TrackModification();
				}

				return removedEntries;
			}
		}

		public void Clear()
		{
			lock (_InternalLock)	// Write operation
			{
				_Version++;
				InitializeHashtable(_InitialCapacity);
				InitializeHeap(_InitialCapacity);
				TrackModification();
			}
		}

		public bool ContainsKey(TKey key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			lock (_InternalLock)	// Read operation
			{
				if (HashtableCount == 0)
				{
					return false;
				}

				int heapIndex = FindHeapIndexInHashtable(key);

				return (heapIndex >= 0);
			}
		}

		public bool ContainsOverdue
		{
			get
			{
				lock (_InternalLock)	// Read with possible write operation
				{
					if (IsDeletedEntryAtHead)
					{
						PruneDeletedEntries();
					}

					if (HashtableCount == 0)
					{
						return false;
					}

					return (_HeapEntries[0].IsOverdue);
				}
			}
		}

		public bool TryDequeue(out TValue result)
		{
			HeapEntry appointment;
			if (TryRemoveHeapEntry(out appointment))
			{
				result = appointment.Value;
				return true;
			}

			result = default(TValue);
			return false;
		}

		public bool TryPeek(out TValue result)
		{
			lock (_InternalLock)	// Read with possible write operation
			{
				if (IsDeletedEntryAtHead)
				{
					PruneDeletedEntries();
				}

				if (HashtableCount == 0)
				{
					result = default(TValue);
					return false;
				}

				result = _HeapEntries[0].Value;
				return true;
			}
		}

		public bool TryPeekNextDue(out DateTime result)
		{
			lock (_InternalLock)	// Read with possible write operation
			{
				if (IsDeletedEntryAtHead)
				{
					PruneDeletedEntries();
				}

				if (HashtableCount == 0)
				{
					result = default(DateTime);
					return false;
				}

				result = _HeapEntries[0].Due;
				return true;
			}
		}

		public int Count
		{
			get
			{
				return Thread.VolatileRead(ref _HashtableSize) - Thread.VolatileRead(ref _HashtableFreeBucketCount);
			}
		}

		public bool IsEmpty
		{
			get
			{
				return (this.Count == 0);
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		bool ICollection.IsSynchronized
		{
			get
			{
				return true;
			}
		}

		object ICollection.SyncRoot
		{
			get
			{
				if (_SyncRoot == null)
				{
					Interlocked.CompareExchange<object>(ref _SyncRoot, new object(), null);
				}

				return _SyncRoot;
			}
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			long startingVersion;
			int startingSize;

			lock (_InternalLock)	// Read with possible write operation
			{
				if (IsDeletedEntryAtHead)
				{
					PruneDeletedEntries();
				}

				if (!_IsHeapOrdered)
				{
					SortHeap();
				}

				startingVersion = _Version;
				startingSize = HeapCount;
			}

			for (int heapIndex = 0; heapIndex < startingSize; heapIndex++)
			{
				if (startingVersion != _Version)
				{
					throw new InvalidOperationException("The collection was modified; enumeration operation may not execute.");
				}

				HeapEntry appointment = _HeapEntries[heapIndex];

				if (appointment.IsDeleted)
				{
					continue;
				}

				yield return new KeyValuePair<TKey, TValue>(appointment.Key, appointment.Value);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.Values.GetEnumerator();
		}

		public IEnumerable<TKey> Keys
		{
			get
			{
				// Optimistic concurrency for this idempotent and inexpensive operation 
				if (_KeyEnumerable == null)
				{
					_KeyEnumerable = new ScheduledDictionary<TKey, TValue>.KeyEnumerable(this);
				}

				return _KeyEnumerable;
			}
		}

		public IEnumerable<TValue> Values
		{
			get
			{
				// Optimistic concurrency for this idempotent and inexpensive operation 
				if (_ValueEnumerable == null)
				{
					_ValueEnumerable = new ScheduledDictionary<TKey, TValue>.ValueEnumerable(this);
				}

				return _ValueEnumerable;
			}
		}

		public IEnumerable<TValue> OverdueValues
		{
			get
			{
				// Optimistic concurrency for this idempotent and inexpensive operation 
				if (_OverdueValueEnumerable == null)
				{
					_OverdueValueEnumerable = new ScheduledDictionary<TKey, TValue>.OverdueValueEnumerable(this);
				}

				return _OverdueValueEnumerable;
			}
		}

		public TValue[] ToArray()
		{
			lock (_InternalLock)	// Read with possible write operation
			{
				if (IsDeletedEntryAtHead)
				{
					PruneDeletedEntries();
				}

				int currentHeapSize = HeapCount;

				if (currentHeapSize == 0)
				{
					return new TValue[] { };
				}

				TValue[] tValueArray = new TValue[currentHeapSize];

				for (int heapIndex = 0; heapIndex < currentHeapSize; heapIndex++)
				{
					tValueArray[heapIndex] = _HeapEntries[heapIndex].Value;
				}

				return tValueArray;
			}
		}

		public void CopyTo(TValue[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}

			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}

			lock (_InternalLock)	// Read with possible write operation
			{
				if (IsDeletedEntryAtHead)
				{
					PruneDeletedEntries();
				}

				int currentHeapSize = HeapCount;

				if (currentHeapSize == 0)
				{
					return;
				}

				if (index + currentHeapSize > array.Length)
				{
					throw new ArgumentException("Destination array is not large enough to fit the contents of the dictionary.");
				}

				for (int heapIndex = 0; heapIndex < currentHeapSize; heapIndex++)
				{
					array.SetValue(_HeapEntries[heapIndex].Value, heapIndex + index);
				}
			}
		}

		#endregion Public Members

		#region Internal Members

		private void TrackModification()
		{
			if (_StateTransitionAction != null)
			{
				_StateTransitionAction();
			}
		}

		#endregion Internal Members

		#region Heap

		private int HeapCount
		{
			get
			{
				return _HashtableSize - _HashtableFreeBucketCount + _PendingDeleteCount;
			}
		}

		private void InitializeHeap(int capacity)
		{
			_HeapEntries = new HeapEntry[capacity];
			_PendingDeleteCount = 0;
		}

		private int InsertIntoHeap(TKey key, TValue value, DateTime due)
		{
			int nextAvailableHeapIndex = HeapCount;
			EnsureHeapCapacity();
			_HeapEntries[nextAvailableHeapIndex] = new HeapEntry(due, _Version, key, value);
			InsertIntoHashtable(key, nextAvailableHeapIndex);
			return TrickleUp(nextAvailableHeapIndex);
		}

		private bool TryRemoveHeapEntry(out HeapEntry result)
		{
			lock (_InternalLock)	// Write operation
			{
				PruneDeletedEntries();

				if (HashtableCount == 0 || !_HeapEntries[0].IsOverdue)
				{
					result = default(HeapEntry);
					return false;
				}

				int lastHeapIndex = HeapCount - 1;
				HeapEntry nextAppointment = _HeapEntries[0];
				HeapEntry followingAppointment = _HeapEntries[lastHeapIndex];

				_Version++;
				DeleteFromHashtable(nextAppointment.Key);

				if (lastHeapIndex > 0)
				{
					DeleteFromHashtable(followingAppointment.Key);
					_HeapEntries[lastHeapIndex] = null;
					_HeapEntries[0] = followingAppointment;
					InsertIntoHashtable(followingAppointment.Key, 0);
					TrickleDown(lastHeapIndex);
				}
				else
				{
					_HeapEntries[lastHeapIndex] = null;
				}

				LimitHeapSparsity();
				result = nextAppointment;
				TrackModification();
				return true;
			}
		}

		private bool IsDeletedEntryAtHead
		{
			get
			{
				return (HeapCount > 0 && _HeapEntries[0].IsDeleted);
			}
		}

		private void PruneDeletedEntries()
		{
			bool deletedEntriesRemoved = false;

			while (IsDeletedEntryAtHead)
			{
				if (!deletedEntriesRemoved)
				{
					_Version++;
					deletedEntriesRemoved = true;
				}

				int lastHeapIndex = HeapCount - 1;
				HeapEntry followingAppointment = _HeapEntries[lastHeapIndex];

				if (lastHeapIndex > 0)
				{
					if (!followingAppointment.IsDeleted)
					{
						DeleteFromHashtable(followingAppointment.Key);
					}

					_HeapEntries[lastHeapIndex] = null;
					_HeapEntries[0] = followingAppointment;
					_PendingDeleteCount--;

					if (!followingAppointment.IsDeleted)
					{
						InsertIntoHashtable(followingAppointment.Key, 0);
					}

					TrickleDown(lastHeapIndex);
				}
				else
				{
					_HeapEntries[lastHeapIndex] = null;
					_PendingDeleteCount--;
				}
			}

			if (deletedEntriesRemoved)
			{
				LimitHeapSparsity();
			}
		}

		private int PruneOverdueEntries()
		{
			bool deletedEntriesRemoved = false;
			int overdueEntriesRemoved = 0;

			while (HeapCount > 0)
			{
				if (_HeapEntries[0].IsDeleted)
				{
					if (!deletedEntriesRemoved)
					{
						_Version++;
						deletedEntriesRemoved = true;
					}
				}
				else if (_HeapEntries[0].IsOverdue)
				{
					if (overdueEntriesRemoved == 0)
					{
						_Version++;
					}

					overdueEntriesRemoved++;
				}
				else
				{
					break;
				}

				int lastHeapIndex = HeapCount - 1;
				HeapEntry nextAppointment = _HeapEntries[0];
				HeapEntry followingAppointment = _HeapEntries[lastHeapIndex];

				if (lastHeapIndex > 0)
				{
					if (!nextAppointment.IsDeleted)
					{
						DeleteFromHashtable(nextAppointment.Key);
					}

					if (!followingAppointment.IsDeleted)
					{
						DeleteFromHashtable(followingAppointment.Key);
					}

					_HeapEntries[lastHeapIndex] = null;
					_HeapEntries[0] = followingAppointment;

					if (nextAppointment.IsDeleted)
					{
						_PendingDeleteCount--;
					}
					else
					{
						InsertIntoHashtable(followingAppointment.Key, 0);
					}

					TrickleDown(lastHeapIndex);
				}
				else
				{
					if (nextAppointment.IsDeleted)
					{
						_HeapEntries[lastHeapIndex] = null;
						_PendingDeleteCount--;
					}
					else
					{
						DeleteFromHashtable(nextAppointment.Key);
						_HeapEntries[lastHeapIndex] = null;
					}
				}
			}

			if (deletedEntriesRemoved || overdueEntriesRemoved > 0)
			{
				LimitHeapSparsity();
			}

			return overdueEntriesRemoved;
		}

		private int TrickleUp(int heapIndex)
		{
			while (heapIndex > 0)
			{
				int parentHeapIndex = (heapIndex - 1) >> 1;

				if (HeapEntry.Compare(_HeapEntries[heapIndex], _HeapEntries[parentHeapIndex]) < 0)
				{
					_IsHeapOrdered = false;
					SwapHeapPositions(heapIndex, parentHeapIndex);
					heapIndex = parentHeapIndex;
				}
				else
				{
					break;
				}
			}

			return heapIndex;
		}

		private void TrickleDown(int lastHeapIndex)
		{
			_IsHeapOrdered = false;

			int currentHeapIndex = 0;

			do
			{
				int originalHeapIndex = currentHeapIndex;
				int childHeapIndex1 = (currentHeapIndex << 1) + 1;
				int childHeapIndex2 = childHeapIndex1 + 1;

				if (lastHeapIndex > childHeapIndex1 && HeapEntry.Compare(_HeapEntries[currentHeapIndex], _HeapEntries[childHeapIndex1]) > 0)
				{
					currentHeapIndex = childHeapIndex1;
				}

				if (lastHeapIndex > childHeapIndex2 && HeapEntry.Compare(_HeapEntries[currentHeapIndex], _HeapEntries[childHeapIndex2]) > 0)
				{
					currentHeapIndex = childHeapIndex2;
				}

				if (currentHeapIndex == originalHeapIndex)
				{
					break;
				}

				SwapHeapPositions(currentHeapIndex, originalHeapIndex);
			} while (true);
		}

		private void SwapHeapPositions(int indexX, int indexY)
		{
			HeapEntry originalXEntry = _HeapEntries[indexX];
			HeapEntry originalYEntry = _HeapEntries[indexY];

			if (originalXEntry.IsDeleted)
			{
				_HeapEntries[indexY] = originalXEntry;
			}
			else
			{
				_HeapEntries[indexY] = originalXEntry;
				InsertIntoHashtable(originalXEntry.Key, indexY);
			}

			if (originalYEntry.IsDeleted)
			{
				_HeapEntries[indexX] = originalYEntry;
			}
			else
			{
				_HeapEntries[indexX] = originalYEntry;
				InsertIntoHashtable(originalYEntry.Key, indexX);
			}
		}

		private void EnsureHeapCapacity()
		{
			int currentHeapSize = HeapCount;
			if (currentHeapSize >= _HeapEntries.Length)
			{
				int newHeapCapacity = _HeapEntries.Length * 2;

				if (newHeapCapacity < _InitialCapacity)
				{
					newHeapCapacity = _InitialCapacity;
				}
				else if (newHeapCapacity < currentHeapSize + 1)
				{
					newHeapCapacity = currentHeapSize + 1;
				}

				SetHeapCapacity(newHeapCapacity);
			}
		}

		private void LimitHeapSparsity()
		{
			int currentHeapCapacity = _HeapEntries.Length;
			int currentHeapSize = HeapCount;

			if (currentHeapCapacity > 1024 && ((currentHeapSize + 1) * 10) < currentHeapCapacity)
			{
				int newHeapCapacity = (currentHeapSize + 1) * 2;
				SetHeapCapacity(newHeapCapacity);
			}
		}

		private void SetHeapCapacity(int capacity)
		{
			if (capacity != _HeapEntries.Length)
			{
				if (capacity > 0)
				{
					HeapEntry[] newHeapEntries = new HeapEntry[capacity];
					int currentHeapSize = HeapCount;

					if (currentHeapSize > 0)
					{
						Array.Copy(_HeapEntries, 0, newHeapEntries, 0, currentHeapSize);
					}

					_HeapEntries = newHeapEntries;
				}
				else
				{
					_HeapEntries = new HeapEntry[0];
				}
			}
		}

		private void SortHeap()
		{
			int currentHeapSize = HeapCount;

			if (currentHeapSize <= 1)
			{
				_IsHeapOrdered = true;
				return;
			}

			_Version++;
			// Only the non-generic Array.Sort works with HeapEntry
			Array.Sort((Array)_HeapEntries, 0, currentHeapSize);
			_IsHeapOrdered = true;
			RebuildHashTable(_HashtableBuckets.Length, false);
		}

		private sealed class HeapEntry : IComparable, IComparable<HeapEntry>
		{
			public DateTime Due;
			public TKey Key;
			public TValue Value;
			public bool IsDeleted;
			private long _Ticks;
			private long _Tiebreak;

			public HeapEntry(DateTime due, long tiebreak, TKey key, TValue value)
			{
				this.Due = due;
				this.Key = key;
				this.Value = value;
				this._Ticks = due.Ticks;
				this._Tiebreak = tiebreak;
			}

			public bool IsOverdue
			{
				get
				{
					return this._Ticks < DateTime.UtcNow.Ticks;
				}
			}

			public int CompareTo(object other)
			{
				return Compare(this, other as HeapEntry);
			}

			public int CompareTo(HeapEntry other)
			{
				return Compare(this, other);
			}

			public static int Compare(HeapEntry x, HeapEntry y)
			{
				if (x._Ticks < y._Ticks) return -1;
				if (x._Ticks > y._Ticks) return 1;

				// Earlier added entries should be come before later entries due at the same time
				if (x._Tiebreak < y._Tiebreak) return -1;

				return 1;
			}
		}

		#endregion Heap

		#region Hashtable

		private int HashtableCount
		{
			get
			{
				return _HashtableSize - _HashtableFreeBucketCount;
			}
		}

		private void InitializeHashtable(int capacity)
		{
			_HashtableBuckets = new int[capacity];
			_HashtableEntries = new HashtableEntry[capacity];

			for (int i = 0; i < capacity; i++)
			{
				_HashtableBuckets[i] = -1;
			}

			_HashtableNextFreeBucketIndex = -1;
			_HashtableFreeBucketCount = 0;
			_HashtableSize = 0;
			_PendingDeleteCount = 0;
			_IsHeapOrdered = true;
		}

		private int FindHeapIndexInHashtable(TKey key)
		{
			int hashCode = GetHashCode(key);
			int slotIndex = hashCode % _HashtableBuckets.Length;

			for (int bucketIndex = _HashtableBuckets[slotIndex]; bucketIndex >= 0; bucketIndex = _HashtableEntries[bucketIndex].NextBucketIndex)
			{
				if (_HashtableEntries[bucketIndex].HashCode == hashCode && _Comparer.Equals(_HashtableEntries[bucketIndex].Key, key))
				{
					return _HashtableEntries[bucketIndex].HeapIndex;
				}
			}

			return -1;
		}

		private void InsertIntoHashtable(TKey key, int heapIndex)
		{
			int hashCode = GetHashCode(key);
			int slotIndex = hashCode % _HashtableBuckets.Length;
			int collisionCount = 0;

			for (int bucketIndex = _HashtableBuckets[slotIndex]; bucketIndex >= 0; bucketIndex = _HashtableEntries[bucketIndex].NextBucketIndex)
			{
				if (_HashtableEntries[bucketIndex].HashCode == hashCode && _Comparer.Equals(_HashtableEntries[bucketIndex].Key, key))
				{
					// Overwrite existing entry with new heap index
					_HashtableEntries[bucketIndex].HeapIndex = heapIndex;
					return;
				}

				collisionCount++;
			}

			int entryIndex;
			if (_HashtableFreeBucketCount > 0)
			{
				entryIndex = _HashtableNextFreeBucketIndex;
				_HashtableNextFreeBucketIndex = _HashtableEntries[entryIndex].NextBucketIndex;
				_HashtableFreeBucketCount--;
			}
			else
			{
				if (_HashtableSize == _HashtableEntries.Length)
				{
					ExpandHashTableCapacity();
					slotIndex = hashCode % _HashtableBuckets.Length;
				}

				entryIndex = _HashtableSize;
				_HashtableSize++;
			}

			_HashtableEntries[entryIndex].HashCode = hashCode;
			_HashtableEntries[entryIndex].NextBucketIndex = _HashtableBuckets[slotIndex];
			_HashtableEntries[entryIndex].Key = key;
			_HashtableEntries[entryIndex].HeapIndex = heapIndex;
			_HashtableBuckets[slotIndex] = entryIndex;

			if (collisionCount > 100)
			{
				SwitchStringComparer();
			}
		}

		private bool DeleteFromHashtable(TKey key)
		{
			int hashCode = GetHashCode(key);
			int slotIndex = hashCode % _HashtableBuckets.Length;
			int last = -1;

			for (int bucketIndex = _HashtableBuckets[slotIndex]; bucketIndex >= 0; last = bucketIndex, bucketIndex = _HashtableEntries[bucketIndex].NextBucketIndex)
			{
				if (_HashtableEntries[bucketIndex].HashCode == hashCode && _Comparer.Equals(_HashtableEntries[bucketIndex].Key, key))
				{
					if (last < 0)
					{
						_HashtableBuckets[slotIndex] = _HashtableEntries[bucketIndex].NextBucketIndex;
					}
					else
					{
						_HashtableEntries[last].NextBucketIndex = _HashtableEntries[bucketIndex].NextBucketIndex;
					}

					_HashtableEntries[bucketIndex].HashCode = -1;
					_HashtableEntries[bucketIndex].NextBucketIndex = _HashtableNextFreeBucketIndex;
					_HashtableEntries[bucketIndex].Key = default(TKey);
					_HashtableEntries[bucketIndex].HeapIndex = -1;
					_HashtableNextFreeBucketIndex = bucketIndex;
					_HashtableFreeBucketCount++;

					int currentHashtableSize = HashtableCount;

					if (currentHashtableSize > _InitialCapacity && currentHashtableSize < (_HashtableBuckets.Length >> 4))
					{
						ShrinkHashtableCapacity();
					}

					return true;
				}
			}

			return false;
		}

		private void ExpandHashTableCapacity()
		{
			int newHashTableCapacity;

			if (_HashtableBuckets.Length > 1073217534)
			{
				newHashTableCapacity = 2146435069;
			}
			else
			{
				newHashTableCapacity = GetPrime(_HashtableSize * 2);
			}

			RebuildHashTable(newHashTableCapacity, false);
		}

		private void ShrinkHashtableCapacity()
		{
			if (_HashtableBuckets.Length < _InitialCapacity)
			{
				return;
			}

			int newHashTableCapacity = GetPrime(HashtableCount * 2);

			if (newHashTableCapacity < _InitialCapacity)
			{
				newHashTableCapacity = _InitialCapacity;
			}

			RebuildHashTable(newHashTableCapacity, false);
		}

		private void RebuildHashTable(int newHashTableCapacity, bool rebuildHashCodes)
		{
			if (!rebuildHashCodes && newHashTableCapacity > _HashtableBuckets.Length)
			{
				int[] newHashtableBuckets = new int[newHashTableCapacity];
				HashtableEntry[] newHashtableEntries = new HashtableEntry[newHashTableCapacity];

				for (int i = 0; i < newHashTableCapacity; i++)
				{
					newHashtableBuckets[i] = -1;
				}

				Array.Copy(_HashtableEntries, 0, newHashtableEntries, 0, _HashtableSize);

				for (int i = 0; i < _HashtableSize; i++)
				{
					int slotIndex = newHashtableEntries[i].HashCode % newHashTableCapacity;
					newHashtableEntries[i].NextBucketIndex = newHashtableBuckets[slotIndex];
					newHashtableBuckets[slotIndex] = i;
				}

				_HashtableBuckets = newHashtableBuckets;
				_HashtableEntries = newHashtableEntries;
			}
			else if (rebuildHashCodes || newHashTableCapacity < _HashtableBuckets.Length)
			{
				int[] newHashtableBuckets = new int[newHashTableCapacity];
				HashtableEntry[] newHashtableEntries = new HashtableEntry[newHashTableCapacity];

				for (int i = 0; i < newHashTableCapacity; i++)
				{
					newHashtableBuckets[i] = -1;
				}

				HashtableEntry[] oldHashtableEntries = _HashtableEntries;
				_HashtableNextFreeBucketIndex = -1;
				_HashtableFreeBucketCount = 0;
				_HashtableSize = 0;
				_HashtableBuckets = newHashtableBuckets;
				_HashtableEntries = newHashtableEntries;

				for (int i = 0; i < oldHashtableEntries.Length; i++)
				{
					HashtableEntry entry = oldHashtableEntries[i];
					if (entry.HashCode > 0)
					{
						InsertIntoHashtable(entry.Key, entry.HeapIndex);
					}
				}
			}
		}

		private int GetHashCode(TKey key)
		{
			return _Comparer.GetHashCode(key) & 0x7fffffff | 0x1;
		}

		private static int GetPrime(int minimumNumber)
		{
			if (minimumNumber < 0)
			{
				throw new OverflowException("Capacity overflow within hashtable.");
			}

			int precalcIndex = 0;

			while ((precalcIndex < _Primes.Length))
			{
				int precalcValue = _Primes[precalcIndex];

				if (precalcValue >= minimumNumber)
				{
					return precalcValue;
				}

				precalcIndex += 1;
			}

			int candidate = (minimumNumber | 1);

			while (candidate < 0x7fffffff)
			{
				if (IsPrime(candidate))
				{
					return candidate;
				}

				candidate += 2;
			}

			return minimumNumber;
		}

		private static bool IsPrime(int candidate)
		{
			if ((candidate & 1) == 0)
			{
				return (candidate == 2);
			}

			int largestFactor = Convert.ToInt32(Math.Sqrt(Convert.ToDouble(candidate)));
			int divisor = 3;

			while (divisor <= largestFactor)
			{
				if (candidate % divisor == 0)
				{
					return false;
				}

				divisor += 2;
			}

			return true;
		}

		private void SwitchStringComparer()
		{
			if (typeof(TKey) == typeof(string) && _Comparer == System.Collections.Generic.EqualityComparer<string>.Default)
			{
				_Comparer = (IEqualityComparer<TKey>)(IEqualityComparer)new SipHashStringEqualityComparer();
				RebuildHashTable(_HashtableBuckets.Length, true);
			}
		}

		private struct HashtableEntry
		{
			public int HashCode;

			public int NextBucketIndex;

			public TKey Key;

			public int HeapIndex;
		}

		private sealed class SipHashStringEqualityComparer : IEqualityComparer<string>, IEqualityComparer
		{
			private byte[] _Entropy;

			public SipHashStringEqualityComparer()
			{
				_Entropy = new byte[16];

				using (var rng = RandomNumberGenerator.Create())
				{
					rng.GetNonZeroBytes(_Entropy);
				}
			}

			public bool Equals(string x, string y)
			{
				return string.Equals(x, y);
			}

			public int GetHashCode(string obj)
			{
				return (int)SipHash_2_4(obj, _Entropy);
			}

			public new bool Equals(object x, object y)
			{
				if (x == y)
				{
					return true;
				}

				if (x == null || y == null)
				{
					return false;
				}

				string xstr = x as string;

				if (xstr != null)
				{
					string ystr = y as string;

					if (ystr != null)
					{
						return string.Equals(xstr, ystr);
					}
				}

				return object.Equals(x, y);
			}

			public int GetHashCode(object obj)
			{
				if (obj == null)
				{
					return 0;
				}

				string stringValue = obj as string;

				if (stringValue == null)
				{
					return obj.GetHashCode();
				}

				return (int)SipHash_2_4(stringValue, _Entropy);
			}

			public static ulong SipHash_2_4(string obj, byte[] key)
			{
				byte[] inputBytes = Encoding.UTF8.GetBytes(obj);
				ulong k0 = BitConverter.ToUInt64(key, 0);
				ulong k1 = BitConverter.ToUInt64(key, 8);

				int inputLength = inputBytes.Length;

				ulong v0 = 0x736f6d6570736575 ^ k0;
				ulong v1 = 0x646f72616e646f6d ^ k1;
				ulong v2 = 0x6c7967656e657261 ^ k0;
				ulong v3 = 0x7465646279746573 ^ k1;

				ulong b = ((ulong)inputLength) << 56;

				if (inputLength > 0)
				{
					int partialLength = inputLength & 7;
					int mainLength = inputLength - partialLength;

					for (int byteIndex = 0; byteIndex < mainLength; byteIndex += 8)
					{
						ulong m = BitConverter.ToUInt64(inputBytes, byteIndex);
						v3 ^= m;
						SipRound(ref v0, ref v1, ref v2, ref v3);
						SipRound(ref v0, ref v1, ref v2, ref v3);
						v0 ^= m;
					}

					for (int byteIndex = 0; byteIndex < partialLength; byteIndex++)
					{
						b |= unchecked(((ulong)inputBytes[byteIndex + mainLength]) << (byteIndex << 3));
					}
				}

				v3 ^= b;
				SipRound(ref v0, ref v1, ref v2, ref v3);
				SipRound(ref v0, ref v1, ref v2, ref v3);
				v0 ^= b;
				v2 ^= 0xff;

				SipRound(ref v0, ref v1, ref v2, ref v3);
				SipRound(ref v0, ref v1, ref v2, ref v3);
				SipRound(ref v0, ref v1, ref v2, ref v3);
				SipRound(ref v0, ref v1, ref v2, ref v3);

				return v0 ^ v1 ^ v2 ^ v3;
			}

			private static void SipRound(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
			{
				unchecked
				{
					v0 += v1;
					v1 = (v1 << 13) | (v1 >> (64 - 13));
					v1 ^= v0;
					v0 = (v0 << 32) | (v0 >> (64 - 32));

					v2 += v3;
					v3 = (v3 << 16) | (v3 >> (64 - 16));
					v3 ^= v2;

					v0 += v3;
					v3 = (v3 << 21) | (v3 >> (64 - 21));
					v3 ^= v0;

					v2 += v1;
					v1 = (v1 << 17) | (v1 >> (64 - 17));
					v1 ^= v2;
					v2 = (v2 << 32) | (v2 >> (64 - 32));
				}
			}
		}

		#endregion Hashtable

		#region KeyEnumerable

		public sealed class KeyEnumerable : IEnumerable<TKey>
		{
			private ScheduledDictionary<TKey, TValue> _Dictionary;

			public KeyEnumerable(ScheduledDictionary<TKey, TValue> dictionary)
			{
				if (dictionary == null)
				{
					throw new ArgumentNullException("dictionary");
				}

				_Dictionary = dictionary;
			}

			IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
			{
				long startingVersion;
				int startingSize;

				lock (_Dictionary._InternalLock)	// Read with possible write operation
				{
					if (_Dictionary.IsDeletedEntryAtHead)
					{
						_Dictionary.PruneDeletedEntries();
					}

					if (!_Dictionary._IsHeapOrdered)
					{
						_Dictionary.SortHeap();
					}

					startingVersion = _Dictionary._Version;
					startingSize = _Dictionary.HeapCount;
				}

				for (int heapIndex = 0; heapIndex < startingSize; heapIndex++)
				{
					if (startingVersion != _Dictionary._Version)
					{
						throw new InvalidOperationException("The collection was modified; enumeration operation may not execute.");
					}

					HeapEntry appointment = _Dictionary._HeapEntries[heapIndex];

					if (appointment.IsDeleted)
					{
						continue;
					}

					yield return appointment.Key;
				}
			}

			IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<TKey>)this).GetEnumerator();
			}
		}

		#endregion KeyEnumerable

		#region ValueEnumerable

		public sealed class ValueEnumerable : IEnumerable<TValue>
		{
			private ScheduledDictionary<TKey, TValue> _Dictionary;

			public ValueEnumerable(ScheduledDictionary<TKey, TValue> dictionary)
			{
				if (dictionary == null)
				{
					throw new ArgumentNullException("dictionary");
				}

				_Dictionary = dictionary;
			}

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
			{
				long startingVersion;
				int startingSize;

				lock (_Dictionary._InternalLock)	// Read with possible write operation
				{
					if (_Dictionary.IsDeletedEntryAtHead)
					{
						_Dictionary.PruneDeletedEntries();
					}

					if (!_Dictionary._IsHeapOrdered)
					{
						_Dictionary.SortHeap();
					}

					startingVersion = _Dictionary._Version;
					startingSize = _Dictionary.HeapCount;
				}

				for (int heapIndex = 0; heapIndex < startingSize; heapIndex++)
				{
					if (startingVersion != _Dictionary._Version)
					{
						throw new InvalidOperationException("The collection was modified; enumeration operation may not execute.");
					}

					HeapEntry appointment = _Dictionary._HeapEntries[heapIndex];

					if (appointment.IsDeleted)
					{
						continue;
					}

					yield return appointment.Value;
				}
			}

			IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<TValue>)this).GetEnumerator();
			}
		}

		#endregion ValueEnumerable

		#region OverdueValueEnumerable

		public sealed class OverdueValueEnumerable : IEnumerable<TValue>
		{
			private ScheduledDictionary<TKey, TValue> _Dictionary;

			public OverdueValueEnumerable(ScheduledDictionary<TKey, TValue> dictionary)
			{
				if (dictionary == null)
				{
					throw new ArgumentNullException("dictionary");
				}

				_Dictionary = dictionary;
			}

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
			{
				TValue result;

				while (_Dictionary.TryDequeue(out result))
				{
					yield return result;
				}
			}

			IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<TValue>)this).GetEnumerator();
			}
		}

		#endregion OverdueValueEnumerable
	}
}