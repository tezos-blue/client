using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace SLD.Tezos.Client.Security
{
	using Cryptography;

	[Serializable]
	public class PublicKey : ISerializable
	{
		private byte[] data;

		public PublicKey(byte[] data)
		{
			this.data = data;
		}

		public string Hash => CryptoServices.CreatePrefixedHash(HashType.PublicKeyHash, data);

		#region Serialization

		public PublicKey(SerializationInfo info, StreamingContext context)
		{
			this.data = (byte[])info.GetValue("Data", typeof(byte[]));
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Data", data, typeof(byte[]));
		}

		#endregion Serialization

		public override string ToString()
		{
			return CryptoServices.EncodePrefixed(HashType.Public, data);
		}
	}

	[Serializable]
	public sealed class PrivateKey : PhraseProtectedSecret, ISerializable
	{
		private string tempPhrase;

		// SECURITY
		public PrivateKey(byte[] data, string openPhrase) : base(data, openPhrase)
		{
			Debug.Assert(data != null);
			Debug.Assert(openPhrase != null);

			// Temporary...
			tempPhrase = openPhrase;
		}

		internal byte[] AccessData()
		{
			return GetData(tempPhrase);
		}

		#region Serialization

		public PrivateKey(SerializationInfo info, StreamingContext context)
		{
			//this.Data = (byte[])info.GetValue("Data", typeof(byte[]));
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			//info.AddValue("Data", Data, typeof(byte[]));
		}

		#endregion Serialization
	}

	[Serializable]
	public class KeyPair
	{
		public PublicKey PublicKey { get; set; }
		public PrivateKey PrivateKey { get; set; }
	}
}