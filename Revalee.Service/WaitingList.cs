using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Revalee.Service
{
	// This class is a specialized priority queue.
	// Duplicates keys are allowed.
	// Not thread-safe.

	public class WaitingList<T> : ICollection
	{
		private Appointment[] _Heap;
		private readonly int _InitialCapacity;
		private int _Size;
		private long _Version = long.MinValue;
		private object _SyncRoot;
		private bool _IsOrdered = true;

		private sealed class Appointment : IComparable<Appointment>
		{
			private long Ticks;
			private long Tiebreak;

			public Appointment(DateTimeOffset timestamp, long tiebreak, T value)
			{
				this.Timestamp = timestamp;
				this.Ticks = timestamp.UtcTicks;
				this.Tiebreak = tiebreak;
				this.Value = value;
			}

			public DateTimeOffset Timestamp
			{
				get;
				private set;
			}

			public T Value
			{
				get;
				private set;
			}

			public bool IsOverdue
			{
				get
				{
					return this.Ticks < DateTime.UtcNow.Ticks;
				}
			}

			public int CompareTo(Appointment other)
			{
				return Compare(this, other);
			}

			public static int Compare(Appointment x, Appointment y)
			{
				if (x.Ticks < y.Ticks)
				{
					return -1;
				}
				else if (x.Ticks > y.Ticks)
				{
					return 1;
				}
				else
				{
					// Existing tasks should be considered older
					if (x.Tiebreak < y.Tiebreak)
					{
						return -1;
					}
					else
					{
						return 1;
					}
				}
			}
		}

		public WaitingList()
		{
			_InitialCapacity = 0;
			_Heap = new Appointment[_InitialCapacity];
		}

		public WaitingList(int initialCapacity)
		{
			if (initialCapacity < 0)
			{
				throw new ArgumentOutOfRangeException("Initial capacity cannot be negative.", "initialCapacity");
			}

			_InitialCapacity = initialCapacity;
			_Heap = new Appointment[initialCapacity];
		}

		public void Add(T value, DateTime time)
		{
			this.Add(value, new DateTimeOffset(time));
		}

		public void Add(T value, DateTimeOffset time)
		{
			int index = _Size;

			if (index == _Heap.Length)
			{
				EnsureMinimumCapacity(index + 1);
			}

			_Size++;
			_Heap[index] = new Appointment(time, _Version++, value);

			TrickleUp(index);
		}

		public T Dequeue()
		{
			int lastIndex = _Size - 1;
			if (lastIndex == -1)
			{
				throw new InvalidOperationException("Queue is empty.");
			}

			Appointment nextAppt = _Heap[0];
			_Heap[0] = _Heap[lastIndex];
			_Heap[lastIndex] = null;
			_Size = lastIndex;
			_Version++;

			TrickleDown(lastIndex);

			LimitSparsity();

			return nextAppt.Value;
		}

		public T Peek()
		{
			if (_Size == 0)
			{
				throw new InvalidOperationException("Queue is empty.");
			}

			return _Heap[0].Value;
		}

		public void Clear()
		{
			if (_Size > 0)
			{
				if (_Size == _InitialCapacity)
				{
					Array.Clear(_Heap, 0, _Size);
				}
				else
				{
					_Heap = new Appointment[_InitialCapacity];
				}

				_Size = 0;
				_Version++;
			}
			else
			{
				LimitSparsity();
			}
		}

		public int Count
		{
			get { return _Size; }
		}

		public int Capacity
		{
			get
			{
				return _Heap.Length;
			}
			set
			{
				if (value != _Heap.Length)
				{
					if (value < _Size)
					{
						throw new ArgumentOutOfRangeException("Capacity", value, "Specified capacity is too small to fit the contents.");
					}

					if (value > 0)
					{
						Appointment[] tempHeap = new Appointment[value];

						if (_Size > 0)
						{
							Array.Copy(_Heap, 0, tempHeap, 0, _Size);
						}

						_Heap = tempHeap;
					}
					else
					{
						_Heap = new Appointment[0];
					}
				}
			}
		}

		public bool IsEmpty
		{
			get
			{
				return (_Size == 0);
			}
		}

		public DateTimeOffset? PeekNextTime()
		{
			if (_Size == 0)
			{
				return null;
			}
			else
			{
				return _Heap[0].Timestamp;
			}
		}

		public bool ContainsOverdue
		{
			get
			{
				if (_Size == 0)
				{
					return false;
				}
				else
				{
					return (_Heap[0].IsOverdue);
				}
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			EnsureOrderedHeap();
			for (int index = 0; index < _Size; index++)
			{
				yield return _Heap[index].Value;
			}
		}

		private void TrickleUp(int index)
		{
			_IsOrdered = false;

			while (index > 0)
			{
				int parentIndex = (index - 1) >> 1;

				if (Appointment.Compare(_Heap[index], _Heap[parentIndex]) < 0)
				{
					SwapPositions(index, parentIndex);
					index = parentIndex;
				}
				else
				{
					break;
				}
			}
		}

		private void TrickleDown(int lastIndex)
		{
			_IsOrdered = false;

			int currentIndex = 0;

			do
			{
				int originalIndex = currentIndex;
				int childIndex1 = (currentIndex << 1) + 1;
				int childIndex2 = childIndex1 + 1;

				if (lastIndex > childIndex1 && Appointment.Compare(_Heap[currentIndex], _Heap[childIndex1]) > 0)
				{
					currentIndex = childIndex1;
				}

				if (lastIndex > childIndex2 && Appointment.Compare(_Heap[currentIndex], _Heap[childIndex2]) > 0)
				{
					currentIndex = childIndex2;
				}

				if (currentIndex == originalIndex)
				{
					break;
				}

				SwapPositions(currentIndex, originalIndex);
			} while (true);
		}

		private void SwapPositions(int indexX, int indexY)
		{
			Appointment swapArea = _Heap[indexX];
			_Heap[indexX] = _Heap[indexY];
			_Heap[indexY] = swapArea;
		}

		private void EnsureMinimumCapacity(int minimumSize)
		{
			if (_Heap.Length < minimumSize)
			{
				int newCapacity = 4;
				if (_Heap.Length > 2)
				{
					newCapacity = _Heap.Length * 2;
				}

				if (newCapacity < minimumSize)
				{
					newCapacity = minimumSize;
				}

				this.Capacity = newCapacity;
			}
		}

		private void LimitSparsity()
		{
			int currentCapacity = _Heap.Length;
			if (currentCapacity > 1024 && currentCapacity > _InitialCapacity)
			{
				int newUpperLimit = (_Size + 1) * 3;
				if (currentCapacity > newUpperLimit)
				{
					this.Capacity = newUpperLimit;
				}
			}
		}

		private void EnsureOrderedHeap()
		{
			if (!_IsOrdered)
			{
				Array.Sort(_Heap, 0, _Size);
				_IsOrdered = true;
			}
		}

		void ICollection.CopyTo(Array array, int index)
		{
			EnsureOrderedHeap();
			Array.Copy(_Heap, 0, array, index, _Size);
		}

		bool ICollection.IsSynchronized
		{
			get
			{
				return false;
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

		IEnumerator IEnumerable.GetEnumerator()
		{
			EnsureOrderedHeap();
			for (int index = 0; index < _Size; index++)
			{
				yield return _Heap[index].Value;
			}
		}
	}
}