using System;
using System.Linq;
using System.Runtime.Serialization;

namespace SLD.Tezos.Client.Security
{
	using Cryptography;

	[Serializable]
	public class PublicKey : ISerializable
	{
		public PublicKey(byte[] data)
		{
			this.Data = data ?? throw new ArgumentNullException(nameof(data));

			Hash = CryptoServices.CreatePrefixedHash(HashType.PublicKeyHash, Data);
		}

		public byte[] Data { get; private set; }
		public string Hash { get; private set; }

		#region Serialization

		public PublicKey(SerializationInfo info, StreamingContext context)
		{
			this.Data = (byte[])info.GetValue("Data", typeof(byte[]));

			Hash = CryptoServices.CreatePrefixedHash(HashType.PublicKeyHash, Data);
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Data", Data, typeof(byte[]));
		}

		#endregion Serialization

		public override bool Equals(object obj)
			=> obj is PublicKey other ?
				Enumerable.SequenceEqual(other.Data, this.Data) :
				false;

		public override int GetHashCode()
			=> Hash.GetHashCode();

		public override string ToString()
			=> CryptoServices.EncodePrefixed(HashType.Public, Data);
	}

	public sealed class PrivateKey : PhraseProtectedSecret
	{
		public PrivateKey(byte[] data, Passphrase passphrase) : base(data, passphrase)
		{
		}

		private PrivateKey(byte[] encryptedData) : base(encryptedData)
		{
		}

		public byte[] EncryptedData => Data;

		internal static PrivateKey Restore(byte[] encryptedData)
		{
			return new PrivateKey(encryptedData);
		}

		internal byte[] AccessData(Passphrase passphrase)
		{
			return GetData(passphrase);
		}
	}

	[Serializable]
	public class KeyPair
	{
		public PublicKey PublicKey { get; set; }
		public PrivateKey PrivateKey { get; set; }

		public string PublicID => PublicKey.Hash;

		internal bool CanUnlockWith(Passphrase passphrase)
		{
			try
			{
				var privateData = PrivateKey.AccessData(passphrase);

				return CryptoServices.IsKeyMatch(PublicKey.Data, privateData);
			}
			catch
			{
				return false;
			}
		}
	}
}