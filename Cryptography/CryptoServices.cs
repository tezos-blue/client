using System;
using System.IO;
using System.Security.Cryptography;

namespace SLD.Tezos.Cryptography
{
	using Blake2;
	using Chaos.NaCl;

	public static class CryptoServices
	{
		public static string CreatePrefixedHash(HashType hashType, byte[] data)
		{
			var hash = Hash(data, hashType.Size);

			return EncodePrefixed(hashType, hash);
		}

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

		public static void CreateKeyPair(out byte[] publicKey, out byte[] privateKey)
		{
			var seed = CreateRandomBytes(Ed25519.PrivateKeySeedSizeInBytes);

			Ed25519.KeyPairFromSeed(out publicKey, out privateKey, seed);
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

		public static byte[] CreateRandomBytes(int length)
		{
			var random = new RNGCryptoServiceProvider();
			var bytes = new byte[length];
			random.GetBytes(bytes);

			return bytes;
		}

		public static string CreateSignature(byte[] privateKey, byte[] data)
		{
			var signature = new byte[Ed25519.SignatureSizeInBytes];

			var signatureSegment = new ArraySegment<byte>(signature);
			var dataSegment = new ArraySegment<byte>(data);
			var keySegment = new ArraySegment<byte>(privateKey);

			Ed25519.Sign(signatureSegment, dataSegment, keySegment);

			return EncodePrefixed(HashType.Signature, signature);
		}

		public static byte[] AppendSignature(byte[] privateKey, byte[] data)
		{
			using (var buffer = new MemoryStream())
			{
				buffer.Write(data, 0, data.Length);

				var signature = Ed25519.Sign(data, privateKey);
				buffer.Write(signature, 0, signature.Length);

				return buffer.ToArray();
			}
		}

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
		}

		public static byte[] Decrypt(byte[] data, string openPhrase)
		{
			using (var rgb = new PasswordDeriveBytes(openPhrase, SymmetricSalt))
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
		}

		#endregion Symmetric Encryption
	}
}