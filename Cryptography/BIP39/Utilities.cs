using System;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SLD.Tezos.Cryptography.BIP39
{
	/// <summary>
	/// A Library that provides common functionality between my other Bitcoin Modules
	/// Made by thashiznets@yahoo.com.au
	/// v1.0.0.2
	/// Bitcoin:1ETQjMkR1NNh4jwLuN5LxY7bMsHC9PUPSV
	/// </summary>
	public static class Utilities
	{
		/// <summary>
		/// Calculates the SHA256 32 byte checksum of the input bytes
		/// </summary>
		/// <param name="input">bytes input to get checksum</param>
		/// <param name="offset">where to start calculating checksum</param>
		/// <param name="length">length of the input bytes to perform checksum on</param>
		/// <returns>32 byte array checksum</returns>
		public static byte[] Sha256Digest(byte[] input, int offset, int length)
		{
			var sha = new SHA256CryptoServiceProvider();

			return sha.ComputeHash(input, offset, length);
		}

		/// <summary>
		/// Calculates the SHA512 64 byte checksum of the input bytes
		/// </summary>
		/// <param name="input">bytes input to get checksum</param>
		/// <param name="offset">where to start calculating checksum</param>
		/// <param name="length">length of the input bytes to perform checksum on</param>
		/// <returns>64 byte array checksum</returns>
		public static byte[] Sha512Digest(byte[] input, int offset, int length)
		{
			var sha = new SHA512CryptoServiceProvider();

			return sha.ComputeHash(input, offset, length);
		}

		/// <summary>
		/// Converts a hex based string into its bytes contained in a byte array
		/// </summary>
		/// <param name="hex">The hex encoded string</param>
		/// <returns>the bytes derived from the hex encoded string</returns>
		public static byte[] HexStringToBytes(string hexString)
		{
			return Enumerable.Range(0, hexString.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(hexString.Substring(x, 2), 16)).ToArray();
		}

		// <summary>
		/// Turns a byte array into a Hex encoded string
		/// </summary>
		/// <param name="bytes">The bytes to encode to hex</param>
		/// <returns>The hex encoded representation of the bytes</returns>
		public static string BytesToHexString(byte[] bytes, bool upperCase = false)
		{
			if (upperCase)
			{
				return string.Concat(bytes.Select(byteb => byteb.ToString("X2")).ToArray());
			}
			else
			{
				return string.Concat(bytes.Select(byteb => byteb.ToString("x2")).ToArray());
			}
		}

		/// <summary>
		/// Calculates the 64 byte checksum in accordance with HMAC-SHA512
		/// </summary>
		/// <param name="input">The bytes to derive the checksum from</param>
		/// <param name="offset">Where to start calculating checksum in the input bytes</param>
		/// <param name="length">Length of buytes to use to calculate checksum</param>
		/// <param name="hmacKey">HMAC Key used to generate the checksum (note differing HMAC Keys provide unique checksums)</param>
		/// <returns></returns>
		public static byte[] HmacSha512Digest(byte[] input, int offset, int length, byte[] hmacKey)
		{
			var hmac = new HMACSHA512(hmacKey);

			return hmac.ComputeHash(input, offset, length);
		}

		/// <summary>
		/// Merges two byte arrays
		/// </summary>
		/// <param name="source1">first byte array</param>
		/// <param name="source2">second byte array</param>
		/// <returns>A byte array which contains source1 bytes followed by source2 bytes</returns>
		public static Byte[] MergeByteArrays(Byte[] source1, Byte[] source2)
		{
			//Most efficient way to merge two arrays this according to http://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
			Byte[] buffer = new Byte[source1.Length + source2.Length];
			System.Buffer.BlockCopy(source1, 0, buffer, 0, source1.Length);
			System.Buffer.BlockCopy(source2, 0, buffer, source1.Length, source2.Length);

			return buffer;
		}

		/// <summary>
		/// This switches the Endianess of the provided byte array, byte per byte we do bit swappy.
		/// </summary>
		/// <param name="bytes">Bytes to change endianess of</param>
		/// <returns>Bytes with endianess swapped</returns>
		public static byte[] SwapEndianBytes(byte[] bytes)
		{
			byte[] output = new byte[bytes.Length];

			int index = 0;

			foreach (byte b in bytes)
			{
				byte[] ba = { b };
				BitArray bits = new BitArray(ba);

				int newByte = 0;
				if (bits.Get(7)) newByte++;
				if (bits.Get(6)) newByte += 2;
				if (bits.Get(5)) newByte += 4;
				if (bits.Get(4)) newByte += 8;
				if (bits.Get(3)) newByte += 16;
				if (bits.Get(2)) newByte += 32;
				if (bits.Get(1)) newByte += 64;
				if (bits.Get(0)) newByte += 128;

				output[index] = Convert.ToByte(newByte);

				index++;
			}

			//I love lamp
			return output;
		}

		/// <summary>
		/// Normalises a string with NKFD normal form
		/// </summary>
		/// <param name="toNormalise">String to be normalised</param>
		/// <returns>Normalised string</returns>
		public static String NormaliseStringNfkd(String toNormalise)
		{
			return toNormalise.Normalize(NormalizationForm.FormKD);
		}
	}
}