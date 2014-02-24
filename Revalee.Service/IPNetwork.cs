using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Revalee.Service
{
	public class IPNetwork
	{
		protected readonly IPAddress _NetworkAddress;
		protected readonly int _CIDR;
		protected readonly byte[] _Mask;

		public IPNetwork(IPAddress address)
		{
			if (address == null)
			{
				throw new ArgumentNullException("address");
			}

			int addressBitLength;

			if (address.AddressFamily == AddressFamily.InterNetwork)
			{
				addressBitLength = 32;
			}
			else if (address.AddressFamily == AddressFamily.InterNetworkV6)
			{
				addressBitLength = 128;
			}
			else
			{
				throw new FormatException("Unsupported network address family.");
			}

			_NetworkAddress = address;
			_CIDR = addressBitLength;
			_Mask = GenerateMask(addressBitLength, addressBitLength);
		}

		public IPNetwork(IPAddress address, int cidr)
		{
			if (address == null)
			{
				throw new ArgumentNullException("address");
			}

			int addressBitLength;

			if (address.AddressFamily == AddressFamily.InterNetwork)
			{
				addressBitLength = 32;
			}
			else	 if (address.AddressFamily == AddressFamily.InterNetworkV6)
			{
				addressBitLength = 128;
			}
			else
			{
				throw new FormatException("Unsupported network address family.");
			}

			if (cidr < 0 || cidr > addressBitLength)
			{
				throw new ArgumentOutOfRangeException("cidr");
			}

			_NetworkAddress = address;
			_CIDR = cidr;
			_Mask = GenerateMask(cidr, addressBitLength);
		}

		public static IPNetwork Parse(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentNullException("value");
			}

			string[] parts = value.Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);

			switch (parts.Length)
			{
				case 0:
					throw new ArgumentNullException("value");

				case 1:
					IPAddress singleIPAddress = IPAddress.Parse(parts[0]);

					if (singleIPAddress.AddressFamily != AddressFamily.InterNetwork && singleIPAddress.AddressFamily != AddressFamily.InterNetworkV6)
					{
						throw new FormatException("Unsupported network address family.");
					}

					return new IPNetwork(singleIPAddress);

				case 2:
					IPAddress blockIPAddress = IPAddress.Parse(parts[0]);
					int cidr = int.Parse(parts[1], NumberStyles.None, NumberFormatInfo.InvariantInfo);
					return new IPNetwork(blockIPAddress, cidr);

				default:
					throw new FormatException(string.Format("An invalid IP address was specified: {0}", value));
			}
		}

		public static bool TryParse(string value, out IPNetwork network)
		{
			if (string.IsNullOrEmpty(value))
			{
				network = null;
				return false;
			}

			string[] parts = value.Split(new char[] { '/' }, 2, StringSplitOptions.RemoveEmptyEntries);

			switch ( parts.Length)
			{
				case 1:
					IPAddress singleIPAddress;
					if (IPAddress.TryParse(parts[0], out singleIPAddress))
					{
						if (singleIPAddress.AddressFamily == AddressFamily.InterNetwork)
						{
							network = new IPNetwork(singleIPAddress);
							return true;
						}
						else if (singleIPAddress.AddressFamily == AddressFamily.InterNetworkV6)
						{
							network = new IPNetwork(singleIPAddress);
							return true;
						}
					}
					break;

				case 2:
					IPAddress blockIPAddress = null;
					int cidr = 0;

					if (IPAddress.TryParse(parts[0], out blockIPAddress) && int.TryParse(parts[1], NumberStyles.None, NumberFormatInfo.InvariantInfo, out cidr))
					{
						if (blockIPAddress.AddressFamily == AddressFamily.InterNetwork && cidr >= 0 && cidr <= 32)
						{
							network = new IPNetwork(blockIPAddress, cidr);
							return true;
						}
						else if (blockIPAddress.AddressFamily == AddressFamily.InterNetworkV6 && cidr >= 0 && cidr <= 128)
						{
							network = new IPNetwork(blockIPAddress, cidr);
							return true;
						}
					}
					break;
			}

			network = null;
			return false;
		}

		public bool IsAddressInNetwork(IPAddress address)
		{
			if (address == null)
			{
				throw new ArgumentNullException("address");
			}

			if (address.AddressFamily != _NetworkAddress.AddressFamily)
			{
				return false;
			}

			if (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6)
			{
				byte[] incomingAddress = address.GetAddressBytes();
				byte[] networkAddress = _NetworkAddress.GetAddressBytes();

				for (int byteIndex = 0; byteIndex < incomingAddress.Length; byteIndex++)
				{
					if ((incomingAddress[byteIndex] & _Mask[byteIndex]) != (networkAddress[byteIndex] & _Mask[byteIndex]))
					{
						return false;
					}
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		public override bool Equals(object obj)
		{
			IPNetwork otherIPNetwork = obj as IPNetwork;
			if (otherIPNetwork == null)
			{
				return false;
			}

			return (_CIDR == otherIPNetwork._CIDR && _NetworkAddress.Equals(otherIPNetwork._NetworkAddress));
		}

		public override int GetHashCode()
		{
			return (_NetworkAddress.GetHashCode() ^ _CIDR);
		}

		public override string ToString()
		{
			return string.Concat(_NetworkAddress.ToString(), "/", _CIDR.ToString());
		}

		private static byte[] GenerateMask(int cidr, int addressBitLength)
		{
			Debug.Assert(addressBitLength >= 8 && addressBitLength <= 128);
			Debug.Assert(addressBitLength % 8 == 0);
			Debug.Assert(cidr >= 0 && cidr <= addressBitLength);

			var bits = new BitArray(addressBitLength);

			for (int bitIndex = 0; bitIndex < cidr; bitIndex++)
			{
				bits[bitIndex] = true;
			}

			var byteArray = new byte[addressBitLength >> 3];
			bits.CopyTo(byteArray, 0);
			return byteArray;
		}
	}
}