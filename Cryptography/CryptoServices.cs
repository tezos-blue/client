using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SLD.Tezos.Cryptography
{
	using Blake2;
	using SLD.Tezos.Cryptography.NaCl;
	using BIP39;

	public static class CryptoServices
	{
		#region Random

		public static byte[] CreateRandomBytes(int length)
		{
			var bytes = new byte[length];

			FillRandom(bytes);

			return bytes;
		}

		public static void FillRandom(byte[] bytes)
		{
			var random = new RNGCryptoServiceProvider();
			random.GetBytes(bytes);
		}

		#endregion Random

		#region Encoding

		public static string EncodePrefixed(HashType hashType, byte[] hash)
		{
			var stream = new MemoryStream();

			stream.Write(hashType.Prefix, 0, hashType.Prefix.Length);
			stream.Write(hash, 0, hash.Length);

			return Base58Check.Encode(stream.ToArray());
		}

		public static byte[] DecodePrefixed(HashType hashType, string encoded)
		{
			var raw = Base58Check.Decode(encoded);

			var data = new byte[raw.Length - hashType.Prefix.Length];

			Array.Copy(raw, hashType.Prefix.Length, data, 0, data.Length);

			return data;
		}

		#endregion Encoding

		#region Keys

		public static void CreateKeyPair(out byte[] publicKey, out byte[] privateKey)
		{
			var seed = CreateRandomBytes(Ed25519.PrivateKeySeedSizeInBytes);

			Ed25519.KeyPairFromSeed(out publicKey, out privateKey, seed);
		}

		public static bool IsKeyMatch(byte[] publicKey, byte[] privateKey)
		{
			ArraySegment<byte> lowerHalf = PublicFromPrivate(privateKey);

			return Enumerable.SequenceEqual(lowerHalf, publicKey);
		}

		public static (byte[], byte[]) ImportEd25519(string edsk)
		{
			var privateKey = DecodePrefixed(HashType.Private, edsk);

			return (PublicFromPrivate(privateKey).ToArray(), privateKey);
		}

		public static (byte[], byte[]) ImportBIP39(string words, string email, string passphrase)
		{
			var bip = new BIP39.BIP39(words, email + passphrase);

			var privateKey = bip.SeedBytes;

			return (PublicFromPrivate(privateKey).ToArray(), privateKey);
		}

		public static bool IsValidEd25519(string edsk)
		{
			try
			{
				ImportEd25519(edsk);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static ArraySegment<byte> PublicFromPrivate(byte[] privateKey)
		{
			var halfSize = Ed25519.ExpandedPrivateKeySizeInBytes / 2;

			var lowerHalf = new ArraySegment<byte>(privateKey, halfSize, halfSize);

			return lowerHalf;
		}

		#endregion Keys

		#region Hashes

		public static string CreateAccountHash(string operationHash, uint originateIndex)
		{
			var buffer = new byte[36];

			DecodePrefixed(HashType.Operation, operationHash)
				.CopyTo(buffer, 0);

			BitConverter.GetBytes(originateIndex)
				.CopyTo(buffer, 32);

			var hash = Hash(buffer, HashType.Account.Size);

			return EncodePrefixed(HashType.Account, hash);
		}

		public static string CreatePrefixedHash(HashType hashType, byte[] data)
		{
			var hash = Hash(data, hashType.Size);

			return EncodePrefixed(hashType, hash);
		}

		public static byte[] Hash(byte[] data, int size)
		{
			var hasher = Blake2B.Create(new Blake2BConfig
			{
				OutputSizeInBytes = size,
			});

			hasher.Init();
			hasher.Update(data);
			return hasher.Finish();
		}

		#endregion Hashes

		#region Signatures

		public static string CreateEncodeSignature(byte[] privateKey, byte[] data)
			=> EncodePrefixed(HashType.Signature, CreateSignature(privateKey, data));

		public static byte[] CreateSignature(byte[] privateKey, byte[] data)
			=> Ed25519.Sign(data, privateKey);

		#endregion Signatures

		#region Memory Protection

		// SECURITY
		// in memory encryption with per-process key
		private static AesManaged aesMemory = new AesManaged();

		internal static byte[] Unscramble(byte[] encryptedData)
		{
			// Create a decryptor to perform the stream transform.
			ICryptoTransform decryptor = aesMemory.CreateDecryptor();

			// Create the streams used for decryption.
			using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
			{
				using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
				{
					using (var decrypted = new MemoryStream())
					{
						csDecrypt.CopyTo(decrypted);

						return decrypted.ToArray();
					}
				}
			}
		}

		internal static byte[] Scramble(byte[] data)
		{
			// Create an encryptor to perform the stream transform.
			ICryptoTransform encryptor = aesMemory.CreateEncryptor();

			// Create the streams used for encryption.
			using (MemoryStream encrypted = new MemoryStream())
			{
				using (CryptoStream csEncrypt = new CryptoStream(encrypted, encryptor, CryptoStreamMode.Write))
				{
					csEncrypt.Write(data, 0, data.Length);
					csEncrypt.FlushFinalBlock();
				}

				return encrypted.ToArray();
			}
		}

		#endregion Memory Protection

		#region Symmetric Encryption

		private static readonly byte[] SymmetricSalt = { 1, 2, 3, 4 };

		public static byte[] Encrypt(byte[] data, string openPhrase)
		{
			using (var rgb = new PasswordDeriveBytes(openPhrase, SymmetricSalt))
			{
				return Encrypt(data, rgb);
			}
		}

		public static byte[] Encrypt(byte[] data, PasswordDeriveBytes rgb)
		{
			var algorithm = new AesManaged();

			byte[] rgbKey = rgb.GetBytes(algorithm.KeySize >> 3);
			byte[] rgbIV = rgb.GetBytes(algorithm.BlockSize >> 3);

			using (ICryptoTransform transform = algorithm.CreateEncryptor(rgbKey, rgbIV))
			using (MemoryStream encrypted = new MemoryStream())
			using (CryptoStream crypto = new CryptoStream(encrypted, transform, CryptoStreamMode.Write))
			{
				crypto.Write(data, 0, data.Length);
				crypto.FlushFinalBlock();

				return encrypted.ToArray();
			}
		}

		public static byte[] Decrypt(byte[] data, string openPhrase)
		{
			using (var rgb = new PasswordDeriveBytes(openPhrase, SymmetricSalt))
			{
				return Decrypt(data, rgb);
			}
		}

		public static byte[] Decrypt(byte[] data, PasswordDeriveBytes rgb)
		{
			var algorithm = new AesManaged();

			byte[] rgbKey = rgb.GetBytes(algorithm.KeySize >> 3);
			byte[] rgbIV = rgb.GetBytes(algorithm.BlockSize >> 3);

			using (ICryptoTransform transform = algorithm.CreateDecryptor(rgbKey, rgbIV))
			using (MemoryStream encrypted = new MemoryStream(data))
			using (CryptoStream crypto = new CryptoStream(encrypted, transform, CryptoStreamMode.Read))
			using (MemoryStream decrypted = new MemoryStream())
			{
				crypto.CopyTo(decrypted);

				return decrypted.ToArray();
			}
		}

		#endregion Symmetric Encryption
	}
}