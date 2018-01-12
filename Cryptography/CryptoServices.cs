using System;
using System.IO;
using System.Security.Cryptography;

namespace SLD.Tezos.Cryptography
{
	using Blake2;
	using Chaos.NaCl;
	using SLD.Tezos.Client.Security;
	using System.Threading.Tasks;

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

		public static KeyPair CreateKeyPair()
		{
			CreateKeyPair(out byte[] publicKey, out byte[] privateKey);

			return new KeyPair
			{
				PublicKey = new PublicKey(publicKey),
				PrivateKey = new PrivateKey(privateKey),
			};
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

		public static string CreateSignature(PrivateKey privateKey, byte[] data)
		{
			var signature = new byte[Ed25519.SignatureSizeInBytes];

			var signatureSegment = new ArraySegment<byte>(signature);
			var dataSegment = new ArraySegment<byte>(data);
			var keySegment = new ArraySegment<byte>(privateKey.AccessData());

			Ed25519.Sign(signatureSegment, dataSegment, keySegment);

			return EncodePrefixed(HashType.Signature, signature);
		}
		public static byte[] AppendSignature(PrivateKey privateKey, byte[] data)
		{
			using (var buffer = new MemoryStream())
			{
				buffer.Write(data, 0, data.Length);

				var signature = Ed25519.Sign(data, privateKey.AccessData());
				buffer.Write(signature, 0, signature.Length);

				return buffer.ToArray();
			}
		}

		internal static byte[] DecryptUser(byte[] encryptedData)
		{
			return encryptedData;
		}

		internal static byte[] EncryptUser(byte[] data)
		{
			return data;
		}
	}
}