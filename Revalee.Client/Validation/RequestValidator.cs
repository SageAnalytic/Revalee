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
using System.Security.Cryptography;
using System.Text;
using System.Web.Hosting;

namespace Revalee.Client.Validation
{
	/// <summary>
	/// Helper methods to cryptographically ensure that callbacks are only processed when legitimately requested by this application.
	/// </summary>
	public static class RequestValidator
	{
		private const string _DefaultClientKey = "Revalee.Authorization";
		private const short _CurrentVersion = 2;

		/// <summary>
		/// Creates a cipher to be used to validate legitimate callbacks.
		/// </summary>
		/// <param name="callbackUri">An absolute <see cref="T:System.Uri"/> that will be requested on the callback.</param>
		/// <returns>A cipher value for this callback.</returns>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="callbackUri" /> is null.</exception>
		public static string Issue(Uri callbackUri)
		{
			if (callbackUri == null || string.IsNullOrEmpty(callbackUri.OriginalString))
			{
				throw new ArgumentNullException("callbackUri");
			}

			short version = CurrentVersion;
			byte[] nonce = GenerateNonce();
			byte[] clientKey = RetrieveClientKey();
			byte[] subject = GetSubject(callbackUri);
			byte[] clientCryptogram = BuildClientCryptogram(nonce, subject, clientKey);

			return new AuthorizationCipher(AuthorizationCipher.CipherSource.Client, version, nonce, clientCryptogram).ToString();
		}

		/// <summary>
		/// Validates the cipher to ensure it represents a legitimately requested callback.
		/// </summary>
		/// <param name="authorizationHeaderValue">A cipher value for this callback.</param>
		/// <param name="callbackId">A <see cref="T:System.Guid"/> that serves as an identifier for the scheduled callback.</param>
		/// <param name="callbackUri">An absolute <see cref="T:System.Uri"/> that will be requested on the callback.</param>
		/// <returns>true if the cipher is valid, false if not.</returns>
		public static bool Validate(string authorizationHeaderValue, Guid callbackId, Uri callbackUri)
		{
			if (string.IsNullOrEmpty(authorizationHeaderValue))
			{
				return false;
			}

			if (Guid.Empty.Equals(callbackId))
			{
				return false;
			}

			if (callbackUri == null || string.IsNullOrEmpty(callbackUri.OriginalString))
			{
				return false;
			}

			AuthorizationCipher incomingCipher;
			if (!AuthorizationCipher.TryParse(authorizationHeaderValue, out incomingCipher) || incomingCipher.Source != AuthorizationCipher.CipherSource.Server)
			{
				return false;
			}

			short version = incomingCipher.Version;

			switch (version)
			{
				case _CurrentVersion:

					byte[] nonce = incomingCipher.Nonce;
					byte[] clientKey = RetrieveClientKey();
					byte[] subject = GetSubject(callbackUri);
					byte[] clientCryptogram = BuildClientCryptogram(nonce, subject, clientKey);
					byte[] responseId = GetResponseId(callbackId);
					byte[] serverCryptogram = BuildServerCryptogram(nonce, clientCryptogram, responseId);

					return AreEqual(serverCryptogram, incomingCipher.Cryptogram);

				default:

					return false;
			}
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
			string key = RevaleeClientSettings.AuthorizationKey;

			if (string.IsNullOrEmpty(key))
			{
				key = HostingEnvironment.SiteName;

				if (string.IsNullOrEmpty(key))
				{
					// Default will be a hard-coded string
					key = _DefaultClientKey;
				}
			}

			return Encoding.UTF8.GetBytes(key);
		}

		private static byte[] GetSubject(Uri callbackUri)
		{
			if (callbackUri.IsDefaultPort)
			{
				// Remove the port number from the callbackUri if it is the default port for the specified protocol
				return Encoding.UTF8.GetBytes(RemoveDefaultPortFromOriginalString(callbackUri));
			}
			else
			{
				return Encoding.UTF8.GetBytes(callbackUri.OriginalString);
			}
		}

		private static string RemoveDefaultPortFromOriginalString(Uri callbackUri)
		{
			string canonicalizedUriString = callbackUri.OriginalString;

			if (callbackUri.Scheme == Uri.UriSchemeHttp)
			{
				int portDelimiterIndex = Uri.UriSchemeHttp.Length + Uri.SchemeDelimiter.Length + callbackUri.Authority.Length;

				if (canonicalizedUriString[portDelimiterIndex] == ':')
				{
					canonicalizedUriString = canonicalizedUriString.Substring(0, portDelimiterIndex) + canonicalizedUriString.Substring(portDelimiterIndex + ":80".Length);
				}
			}
			else if (callbackUri.Scheme == Uri.UriSchemeHttps)
			{
				int portDelimiterIndex = Uri.UriSchemeHttps.Length + Uri.SchemeDelimiter.Length + callbackUri.Authority.Length;

				if (canonicalizedUriString[portDelimiterIndex] == ':')
				{
					canonicalizedUriString = canonicalizedUriString.Substring(0, portDelimiterIndex) + canonicalizedUriString.Substring(portDelimiterIndex + ":443".Length);
				}
			}

			return canonicalizedUriString;
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