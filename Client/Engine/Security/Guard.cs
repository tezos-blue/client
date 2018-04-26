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