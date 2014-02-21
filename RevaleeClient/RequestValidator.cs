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
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Web.Hosting;

namespace SageAnalytic.Revalee
{
	internal static class RequestValidator
	{
		private const string _AppSettingKey = "RevaleeAuthorizationKey";

		private const short _CurrentVersion = 2;

		internal static string Issue(Uri callbackUrl)
		{
			if (callbackUrl == null || string.IsNullOrEmpty(callbackUrl.OriginalString))
			{
				throw new ArgumentNullException("CallbackUri");
			}

			short version = CurrentVersion;
			byte[] nonce = GenerateNonce();
			byte[] clientKey = RetrieveClientKey();
			byte[] subject = GetSubject(callbackUrl);
			byte[] clientCryptogram = BuildClientCryptogram(nonce, subject, clientKey);

			return new AuthorizationCipher(AuthorizationCipher.CipherSource.Client, version, nonce, clientCryptogram).ToString();
		}

		internal static bool Validate(string authorizationHeaderValue, Guid callbackId, Uri callbackUrl)
		{
			if (string.IsNullOrEmpty(authorizationHeaderValue))
			{
				return false;
			}

			if (Guid.Empty.Equals(callbackId))
			{
				return false;
			}

			if (callbackUrl == null || string.IsNullOrEmpty(callbackUrl.OriginalString))
			{
				return false;
			}

			AuthorizationCipher incomingCipher;
			if (!AuthorizationCipher.TryParse(authorizationHeaderValue, out incomingCipher) || incomingCipher.Source != AuthorizationCipher.CipherSource.Server)
			{
				return false;
			}

			short version = incomingCipher.Version;
			byte[] nonce = incomingCipher.Nonce;
			byte[] clientKey = RetrieveClientKey();
			byte[] subject = GetSubject(callbackUrl);
			byte[] clientCryptogram = BuildClientCryptogram(nonce, subject, clientKey);
			byte[] responseId = GetResponseId(callbackId);
			byte[] serverCryptogram = BuildServerCryptogram(nonce, clientCryptogram, responseId);

			return AreEqual(serverCryptogram, incomingCipher.Cryptogram);
		}

		private static short CurrentVersion
		{
			get
			{
				return _CurrentVersion;
			}
		}

		private static byte[] GenerateNonce()
		{
			byte[] nonceBytes = new byte[16];

			using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
			{
				rng.GetBytes(nonceBytes);
			}

			return nonceBytes;
		}

		private static byte[] RetrieveClientKey()
		{
			string key = ConfigurationManager.AppSettings[_AppSettingKey];

			if (string.IsNullOrEmpty(key))
			{
				key = HostingEnvironment.SiteName;

				if (string.IsNullOrEmpty(key))
				{
					// Default will be a hard-coded string
					key = _AppSettingKey;
				}
			}

			return Encoding.UTF8.GetBytes(key);
		}

		private static byte[] GetSubject(Uri callbackUrl)
		{
			return Encoding.UTF8.GetBytes(callbackUrl.OriginalString);
		}

		private static byte[] GetResponseId(Guid callbackId)
		{
			return callbackId.ToByteArray();
		}

		private static byte[] BuildClientCryptogram(byte[] nonce, byte[] subject, byte[] clientKey)
		{
			byte[] contents = new byte[nonce.Length + subject.Length];
			Array.Copy(nonce, 0, contents, 0, nonce.Length);
			Array.Copy(subject, 0, contents, nonce.Length, subject.Length);

			using (var hmac = new HMACSHA256(clientKey))
			{
				return hmac.ComputeHash(contents);
			}
		}

		private static byte[] BuildServerCryptogram(byte[] nonce, byte[] clientCryptogram, byte[] responseId)
		{
			byte[] contents = new byte[nonce.Length + clientCryptogram.Length + responseId.Length];

			Array.Copy(nonce, 0, contents, 0, nonce.Length);
			Array.Copy(clientCryptogram, 0, contents, nonce.Length, clientCryptogram.Length);
			Array.Copy(responseId, 0, contents, nonce.Length + clientCryptogram.Length, responseId.Length);

			using (var hashingAlgorithm = new SHA256Managed())
			{
				return hashingAlgorithm.ComputeHash(contents);
			}
		}

		private static bool AreEqual(byte[] array1, byte[] array2)
		{
			if (array1 == array2)
			{
				return true;
			}

			if (array1 == null || array2 == null || array1.Length != array2.Length)
			{
				return false;
			}

			for (int byteIndex = 0; byteIndex < array1.Length; byteIndex++)
			{
				if (array1[byteIndex] != array2[byteIndex])
				{
					return false;
				}
			}

			return true;
		}
	}
}