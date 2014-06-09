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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Revalee.Client.RecurringTasks
{
	internal class TaskBuilder : IDisposable
	{
		private Uri _CallbackBaseUri;
		private HashAlgorithm _HashAlgorithm;

		internal TaskBuilder()
			: this(null)
		{
		}

		internal TaskBuilder(Uri callbackBaseUri)
		{
			_CallbackBaseUri = callbackBaseUri;
			_HashAlgorithm = SHA1Managed.Create();
		}

		internal ConfiguredTask Create(IClockSource clockSource, PeriodicityType periodicity, int hourOffset, int minuteOffset, Uri url)
		{
			Uri absoluteUrl;

			if (url.IsAbsoluteUri)
			{
				absoluteUrl = url;
			}
			else
			{
				if (_CallbackBaseUri == null)
				{
					throw new InvalidOperationException(string.Format("The recurring task targeting \"{0}\" is not an absolute URL and no callbackBaseUri attribute was supplied.", url));
				}

				if (!Uri.TryCreate(_CallbackBaseUri, url, out absoluteUrl))
				{
					throw new InvalidOperationException(string.Format("The recurring task targeting \"{0}\" is not an absolute URL and it cannot be combined with the callbackBaseUri attribute of \"{1}\".", url, _CallbackBaseUri));
				}
			}

			string identifier = this.CreateTaskIdentifier(periodicity, hourOffset, minuteOffset, absoluteUrl);
			return new ConfiguredTask(identifier, clockSource, periodicity, hourOffset, minuteOffset, absoluteUrl);
		}

		private string CreateTaskIdentifier(PeriodicityType periodicity, int hourOffset, int minuteOffset, Uri url)
		{
			string keyFormat;

			switch (periodicity)
			{
				case PeriodicityType.Hourly:
					keyFormat = "H~XX:{2:00}~{3}";
					break;

				case PeriodicityType.Daily:
					keyFormat = "D~{1:00}:{2:00}~{3}";
					break;

				default:
					keyFormat = "{0}~{1:00}:{2:00}~{3}";
					break;
			}

			string compoundKey = string.Format(CultureInfo.InvariantCulture, keyFormat, (int)periodicity, hourOffset, minuteOffset, url);
			byte[] textBytes = Encoding.UTF8.GetBytes(compoundKey);
			byte[] hashBytes = _HashAlgorithm.ComputeHash(textBytes);
			return ConvertByteArrayToHexadecimalString(hashBytes);
		}

		private static string ConvertByteArrayToHexadecimalString(byte[] bytes)
		{
			char[] charArray = new char[bytes.Length * 2];
			int byteValue;

			for (int index = 0; index < bytes.Length; index++)
			{
				byteValue = bytes[index] >> 4;
				charArray[index * 2] = (char)(55 + byteValue + (((byteValue - 10) >> 31) & -7));
				byteValue = bytes[index] & 0xF;
				charArray[index * 2 + 1] = (char)(55 + byteValue + (((byteValue - 10) >> 31) & -7));
			}

			return new string(charArray);
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_HashAlgorithm != null)
				{
					_HashAlgorithm.Dispose();
					_HashAlgorithm = null;
				}

				GC.SuppressFinalize(this);
			}
		}

		~TaskBuilder()
		{
			this.Dispose(false);
		}
	}
}