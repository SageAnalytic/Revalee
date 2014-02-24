using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Revalee.Service
{
	// This class is a hash table.
	// Duplicates keys are ignored.
	// Not thread-safe.

	public class KeyedList<T> : ICollection<T>, ICollection
	{
		private readonly static int[] _Primes = new int[] {
			3, 7, 11, 0x11, 0x17, 0x1d, 0x25, 0x2f, 0x3b, 0x47, 0x59, 0x6b, 0x83, 0xa3,
			0xc5, 0xef, 0x125, 0x161, 0x1af, 0x209, 0x277, 0x2f9, 0x397, 0x44f, 0x52f, 0x63d, 0x78b,
			0x91d, 0xaf1, 0xd2b, 0xfd1, 0x12fd, 0x16cf, 0x1b65, 0x20e3, 0x2777, 0x2f6f, 0x38ff, 0x446f,
			0x521f, 0x628d, 0x7655, 0x8e01, 0xaa6b, 0xcc89, 0xf583, 0x126a7, 0x1619b, 0x1a857, 0x1fd3b,
			0x26315, 0x2dd67, 0x3701b, 0x42023, 0x4f361, 0x5f0ed, 0x72125, 0x88e31, 0xa443b, 0xc51eb,
			0xec8c1, 0x11bdbf, 0x154a3f, 0x198c4f, 0x1ea867, 0x24ca19, 0x2c25c1, 0x34fa1b, 0x3f928f,
			0x4c4987, 0x5b8b6f, 0x6dda89};

		private const float _LoadFactor = 0.72f;
		private const int _StepConstant = 11;

		private readonly int _InitialSize = 71;
		private object _SyncRoot;
		private int[] _KeyHashCodes;
		private T[] _KeyValues;
		private int _Size;
		private int _Capacity;
		private IEqualityComparer<T> _Comparer;

		public KeyedList()
		{
			Initialize();
		}

		public KeyedList(int capacity)
		{
			_InitialSize = capacity;
			Initialize();
		}

		public KeyedList(IEqualityComparer<T> comparer)
		{
			_Comparer = comparer;
			Initialize();
		}

		public KeyedList(int capacity, IEqualityComparer<T> comparer)
		{
			_InitialSize = capacity;
			_Comparer = comparer;
			Initialize();
		}

		public void Add(T key)
		{
			SetValue(key);
		}

		public bool Remove(T key)
		{
			return DeleteValue(key);
		}

		public bool Contains(T key)
		{
			return (FindSlot(key) >= 0);
		}

		public int Count
		{
			get
			{
				return _Size;
			}
		}

		public void Clear()
		{
			Initialize();
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			Array.Copy(_KeyValues, 0, array, arrayIndex, _Size);
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return ((IEnumerable<T>)_KeyValues).GetEnumerator();
		}

		private void Initialize()
		{
			_Capacity = _InitialSize;

			if (_Capacity < 17)
			{
				_Capacity = 17;
			}
			else
			{
				_Capacity = _Capacity | 1;	// must be odd
			}

			_Size = 0;

			_KeyHashCodes = new int[_Capacity];
			_KeyValues = new T[_Capacity];

			if (_Comparer == null)
			{
				_Comparer = EqualityComparer<T>.Default;
			}
		}

		private void SetValue(T key)
		{
			int slot = FindSlot(key);
			if (slot < 0)
			{
				if (_Size >= (_Capacity * _LoadFactor))
				{
					ExpandCapacity();
				}

				StoreKey(key);
			}
		}

		private bool DeleteValue(T key)
		{
			int slot = FindSlot(key);
			if (slot >= 0)
			{
				EraseKey(slot);

				if (_Capacity > _InitialSize && _Capacity > 1000 && _Size * 10 < _Capacity)
				{
					ShrinkCapacity();
				}

				return true;
			}

			return false;
		}

		private void ExpandCapacity()
		{
			int oldCapacity = _Capacity;
			int[] oldKeyHashCodes = _KeyHashCodes;
			T[] oldKeyValues = _KeyValues;

			_Capacity = GetPrime(oldCapacity * 2 + 1);

			_KeyHashCodes = new int[_Capacity];
			_KeyValues = new T[_Capacity];

			for (int index = 0; index <= oldCapacity - 1; index++)
			{
				if ((oldKeyHashCodes[index] > 0))
				{
					StoreKey(oldKeyValues[index]);
				}
			}
		}

		private void ShrinkCapacity()
		{
			int oldCapacity = _Capacity;
			int[] oldKeyHashCodes = _KeyHashCodes;
			T[] oldKeyValues = _KeyValues;

			_Capacity = GetPrime(_Size * 2 + 1);

			_KeyHashCodes = new int[_Capacity];
			_KeyValues = new T[_Capacity];

			for (int index = 0; index <= oldCapacity - 1; index++)
			{
				if ((oldKeyHashCodes[index] > 0))
				{
					StoreKey(oldKeyValues[index]);
				}
			}
		}

		private int FindSlot(T key)
		{
			int hashCode = _Comparer.GetHashCode(key) & 0x7fffffff | 0x1;
			int slotIndex = hashCode % _Capacity;
			int stepIncrement = _StepConstant - (hashCode % _StepConstant);

			while (_KeyHashCodes[slotIndex] > 0)
			{
				if (_KeyHashCodes[slotIndex] == hashCode && _Comparer.Equals(_KeyValues[slotIndex], key))
				{
					return slotIndex;
				}

				slotIndex = (slotIndex + stepIncrement) % _Capacity;
			}

			return -1;
		}

		private void StoreKey(T key)
		{
			int hashCode = _Comparer.GetHashCode(key) & 0x7fffffff | 0x1;
			int slotIndex = hashCode % _Capacity;
			int stepIncrement = _StepConstant - (hashCode % _StepConstant);

			while (_KeyHashCodes[slotIndex] > 0)
			{
				if (_KeyHashCodes[slotIndex] == hashCode && _Comparer.Equals(_KeyValues[slotIndex], key))
				{
					break;
				}

				slotIndex = (slotIndex + stepIncrement) % _Capacity;
			}

			_KeyHashCodes[slotIndex] = hashCode;
			_KeyValues[slotIndex] = key;
			_Size++;
		}

		private void EraseKey(int slotIndex)
		{
			_KeyHashCodes[slotIndex] = 0;
			_KeyValues[slotIndex] = default(T);
			_Size--;
		}

		private static int GetPrime(int minimumNumber)
		{
			if (minimumNumber < 0)
			{
				throw new ArgumentException("Capacity overflow within hashtable.");
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

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _KeyValues.GetEnumerator();
		}

		void ICollection.CopyTo(Array array, int index)
		{
			Array.Copy(_KeyValues, 0, array, index, _Size);
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
	}
}