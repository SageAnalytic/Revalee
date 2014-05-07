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

using System.Threading;

namespace Revalee.Client.RecurringTasks
{
	internal class ActivationState
	{
		private int _Value;

		public bool IsActive
		{
			get
			{
				return _Value == 1;
			}
			set
			{
				_Value = Interlocked.Exchange(ref _Value, value ? 1 : 0);
			}
		}

		public ActivationState(bool initialValue = false)
		{
			_Value = initialValue ? 1 : 0;
		}

		public bool TransitionToActive()
		{
			return Interlocked.CompareExchange(ref _Value, 1, 0) == 0;
		}

		public bool TransitionToInactive()
		{
			return Interlocked.CompareExchange(ref _Value, 0, 1) == 1;
		}

		public static implicit operator bool(ActivationState state)
		{
			return state._Value == 1;
		}

		public static implicit operator ActivationState(bool isActive)
		{
			return new ActivationState(isActive);
		}

		public override bool Equals(object obj)
		{
			if (obj is bool)
			{
				return this.Equals((bool)obj);
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public bool Equals(bool isActive)
		{
			return (isActive ? 1 : 0) == _Value;
		}

		public static bool operator ==(ActivationState a, bool b)
		{
			return (a._Value == 1) == b;
		}

		public static bool operator !=(ActivationState a, bool b)
		{
			return (a._Value == 1) != b;
		}

		public static bool operator ==(bool a, ActivationState b)
		{
			return a == (b._Value == 1);
		}

		public static bool operator !=(bool a, ActivationState b)
		{
			return a != (b._Value == 1);
		}
	}
}