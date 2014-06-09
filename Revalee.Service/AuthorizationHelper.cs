using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Revalee.Service
{
	internal static class AuthorizationHelper
	{
		private delegate string CipherProcessor(IDictionary<string, string> incomingCipherValues, Guid callbackId);

		internal static string ConstructResponse(string authorizationCipher, Guid callbackId)
		{
			if (string.IsNullOrEmpty(authorizationCipher))
			{
				return null;
			}

			if (Guid.Empty.Equals(callbackId))
			{
				return null;
			}

			IDictionary<string, string> incomingCipherValues = ParseMultiValueHeader(authorizationCipher);

			short version = 0;
			if (!short.TryParse(incomingCipherValues["v"], out version) || version < 1)
			{
				return null;
			}

			CipherProcessor processor = AssignCipherProcessor(version);

			if (processor == null)
			{
				return null;
			}

			return processor(incomingCipherValues, callbackId);
		}

		private static string CipherProcessorVersion2(IDictionary<string, string> incomingCipherValues, Guid callbackId)
		{
			// Create nonce byte array
			string nonceInHex = incomingCipherValues["n"];
			byte[] nonce = ConvertHexToByteArray(nonceInHex);

			if (nonce == null)
			{
				return null;
			}

			// Create client cryptogram byte array
			string clientCryptogramInHex = incomingCipherValues["c"];
			byte[] clientCryptogram = ConvertHexToByteArray(clientCryptogramInHex);

			if (clientCryptogram == null)
			{
				return null;
			}

			// Construct server cryptogram
			byte[] responseId = GetResponseId(callbackId);
			byte[] serverCryptogram = BuildServerCryptogram(nonce, clientCryptogram, responseId);

			// Build cipher string
			var outgoingCipher = new StringBuilder(255);

			outgoingCipher.Append("v=");
			outgoingCipher.Append(incomingCipherValues["v"]);

			outgoingCipher.Append(",n=");
			outgoingCipher.Append(incomingCipherValues["n"]);

			outgoingCipher.Append(",s=");

			for (int byteIndex = 0; byteIndex < serverCryptogram.Length; byteIndex++)
			{
				outgoingCipher.Append(serverCryptogram[byteIndex].ToString("X2", CultureInfo.InvariantCulture));
			}

			return outgoingCipher.ToString();
		}

		private static CipherProcessor AssignCipherProcessor(short version)
		{
			if (version == 2)
			{
				return CipherProcessorVersion2;
			}

			return null;
		}

		private static readonly byte[] _HexNibbles = new byte[] {
			0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7,
			0x8, 0x9, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
			0xff, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf};

		private static byte[] ConvertHexToByteArray(string value)
		{
			if (value == null || value.Length == 0 || (value.Length & 1) != 0)
			{
				return null;
			}

			byte[] bytes = new byte[value.Length >> 1];

			for (int i = 0; i < bytes.Length; i++)
			{
				int highNibbleOffset = value[i << 1] - 48;
				int lowNibbleOffset = value[(i << 1) + 1] - 48;

				if (highNibbleOffset < 0 || highNibbleOffset > 54 || lowNibbleOffset < 0 || lowNibbleOffset > 54)
				{
					return null;
				}

				byte highNibble = _HexNibbles[highNibbleOffset];
				byte lowNibble = _HexNibbles[lowNibbleOffset];

				if (highNibble == 0xff || lowNibble == 0xff)
				{
					return null;
				}

				bytes[i] = (byte)((highNibble << 4) | lowNibble);
			}

			return bytes;
		}

		private static byte[] GetResponseId(Guid callbackId)
		{
			return callbackId.ToByteArray();
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

		private static IDictionary<string, string> ParseMultiValueHeader(string headerValue)
		{
			var dictionary = new Dictionary<string, string>(10);

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