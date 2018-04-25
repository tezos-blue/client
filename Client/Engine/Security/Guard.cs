using System;
using System.IO;

namespace SLD.Tezos.Client.Security
{
	using Cryptography;
	using Protocol;

	public static class Guard
	{
		public static KeyPair CreateKeyPair(Passphrase passphrase)
		{
			CryptoServices.CreateKeyPair(out byte[] publicKey, out byte[] privateKey);

			return new KeyPair(
				new PublicKey(publicKey),
				new PrivateKey(privateKey, passphrase));
		}

		#region Import keys

		public static PublicKey ImportPublicKey(string publicKey)
		{
			var data = GetKeyData(publicKey);

			return new PublicKey(data);
		}

		private static byte[] GetKeyData(string keyString)
		{
			int start;

			// check first three letters
			switch (keyString.Substring(0, 3).ToLower())
			{
				case "tz1":
					start = 3;
					break;

				case "edp":
				case "eds":
					start = 4;
					break;

				default:
					throw new NotImplementedException();
			}

			var data = Base58Check.Decode(keyString);

			using (var stream = new MemoryStream(data, start, data.Length - start, false))
			{
				return stream.ToArray();
			}
		}

		#endregion Import keys

		internal static void ApplySignature(ProtectedTask task, byte[] data, byte[] signature)
		{
			task.Signature = CryptoServices.EncodePrefixed(HashType.Signature, signature);

			using (var buffer = new MemoryStream())
			{
				buffer.Write(data, 0, data.Length);
				buffer.Write(signature, 0, signature.Length);

				task.SignedOperation = buffer.ToArray().ToHexString();
			}
		}

		private static void Trace(string text)
		{
			Tracer.Trace(typeof(Guard), $"§ --- {text}");
		}
	}
}