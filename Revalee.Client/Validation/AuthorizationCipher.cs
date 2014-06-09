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
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Revalee.Client.Validation
{
	internal class AuthorizationCipher
	{
		public CipherSource Source { get; private set; }

		public short Version { get; private set; }

		public byte[] Nonce { get; private set; }

		public byte[] Cryptogram { get; private set; }

		internal enum CipherSource
		{
			Client,
			Server
		}

		private AuthorizationCipher()
		{ }

		public AuthorizationCipher(CipherSource source, short version, byte[] nonce, byte[] cryptogram)
		{
			if (version < 1)
			{
				throw new ArgumentOutOfRangeException("version");
			}

			if (nonce == null || nonce.Length == 0)
			{
				throw new ArgumentNullException("nonce");
			}

			if (cryptogram == null || cryptogram.Length == 0)
			{
				throw new ArgumentNullException("cryptogram");
			}

			this.Source = source;
			this.Version = version;
			this.Nonce = nonce;
			this.Cryptogram = cryptogram;
		}

		public static bool TryParse(string encodedCipher, out AuthorizationCipher decodedCipher)
		{
			if (string.IsNullOrWhiteSpace(encodedCipher))
			{
				decodedCipher = null;
				return false;
			}

			IDictionary<string, string> cipherValues = ParseMultiValueHeader(encodedCipher);

			if (cipherValues.Count < 3)
			{
				decodedCipher = null;
				return false;
			}

			short version = 0;
			if (!short.TryParse(cipherValues["v"], out version) || version < 1)
			{
				decodedCipher = null;
				return false;
			}

			byte[] nonce = ConvertHexadecimalToByteArray(cipherValues["n"]);

			if (nonce == null)
			{
				decodedCipher = null;
				return false;
			}

			CipherSource source;
			byte[] cryptogram;

			if (cipherValues.ContainsKey("s"))
			{
				source = CipherSource.Server;
				cryptogram = ConvertHexadecimalToByteArray(cipherValues["s"]);
			}
			else if (cipherValues.ContainsKey("c"))
			{
				source = CipherSource.Client;
				cryptogram = ConvertHexadecimalToByteArray(cipherValues["c"]);
			}
			else
			{
				decodedCipher = null;
				return false;
			}

			if (cryptogram == null)
			{
				decodedCipher = null;
				return false;
			}

			decodedCipher = new AuthorizationCipher() { Source = source, Version = version, Nonce = nonce, Cryptogram = cryptogram };
			return true;
		}

		public override string ToString()
		{
			var cipher = new StringBuilder(128);
			cipher.Append("v=").Append(this.Version);
			cipher.Append(",n=");
			AppendHexadecimal(cipher, this.Nonce);
			cipher.Append(this.Source == CipherSource.Server ? ",s=" : ",c=");
			AppendHexadecimal(cipher, this.Cryptogram);
			return cipher.ToString();
		}

		private static byte[] ConvertHexadecimalToByteArray(string value)
		{
			if (value == null || value.Length == 0 || (value.Length & 1) != 0)
			{
				return null;
			}

			int byteLength = value.Length >> 1;
			byte[] byteArray = new byte[byteLength];

			for (int byteIndex = 0; byteIndex < byteLength; byteIndex++)
			{
				unchecked
				{
					int digitIndex = byteIndex << 1;
					uint hexDigit = value[digitIndex];
					uint highNibble = hexDigit - '0';

					if (highNibble > 9)
					{
						highNibble = (hexDigit & ~0x20u) - 'A';

						if (highNibble > 5)
						{
							return null;
						}

						highNibble += 10;
					}

					hexDigit = value[++digitIndex];
					uint lowNibble = hexDigit - '0';

					if (lowNibble > 9)
					{
						lowNibble = (hexDigit & ~0x20u) - 'A';

						if (lowNibble > 5)
						{
							return null;
						}

						lowNibble += 10;
					}

					byteArray[byteIndex] = (byte)((highNibble << 4) | lowNibble);
				}
			}

			return byteArray;
		}

		private static void AppendHexadecimal(StringBuilder output, byte[] value)
		{
			const string hexDigits = "0123456789ABCDEF";

			for (int i = 0; i < value.Length; i++)
			{
				int byteValue = value[i];
				output.Append(hexDigits[byteValue >> 4]);
				output.Append(hexDigits[byteValue & 0xF]);
			}
		}

		private static IDictionary<string, string> ParseMultiValueHeader(string headerValue)
		{
			var dictionary = new Dictionary<string, string>();

			string[] parts = headerValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			for (int partIndex = 0; partIndex < parts.Length; partIndex++)
			{
				string part = parts[partIndex];

				int equalIndex = part.IndexOf('=', 0);

				if (equalIndex > 0 && equalIndex < part.Length - 1)
				{
					dictionary.Add(part.Substring(0, equalIndex).Trim(), part.Substring(equalIndex + 1).Trim());
				}
			}

			return dictionary;
		}
	}
}