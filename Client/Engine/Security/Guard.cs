using System;
using System.IO;

namespace SLD.Tezos.Client.Security
{
	using Cryptography;

	public static class Guard
	{
		internal static KeyPair CreateKeyPair()
		{
			byte[] publicKey, privateKey;

			CryptoServices.CreateKeyPair(out publicKey, out privateKey);

			return new KeyPair
			{
				PublicKey = new PublicKey(publicKey),
				PrivateKey = new PrivateKey(privateKey),
			};
		}

		internal static byte[] DecryptUser(byte[] encryptedData)
		{
			return encryptedData;
		}

		#region Import keys

		public static PrivateKey ImportPrivateKey(string privateKey)
		{
			var data = GetKeyData(privateKey);

			return new PrivateKey(data);
		}

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

		private static void Trace(string text)
		{
			Tracer.Trace(typeof(Guard), $"§ --- {text}");
		}
	}
}