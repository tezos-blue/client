using System;
using System.Diagnostics;

namespace SLD.Tezos.Cryptography
{
	// SECURITY
	// All secrets are scrambled while in memory
	public abstract class Secret : IDisposable
	{
		private byte[] inMemory;

		protected Secret()
		{
		}

		protected byte[] Data
		{
			get => CryptoServices.Unscramble(inMemory);
			set => inMemory = CryptoServices.Scramble(value);
		}

		// SECURITY
		// on dispose, data gets overwritten and released to garbage collection
		public void Dispose()
		{
			if (inMemory != null)
			{
				Array.Copy(
					CryptoServices.CreateRandomBytes(inMemory.Length),
					inMemory,
					inMemory.Length);

				inMemory = null;
			}
		}
	}

	public abstract class PhraseProtectedSecret : IDisposable
	{
		private byte[] inMemory;

		protected PhraseProtectedSecret() { }
		protected PhraseProtectedSecret(byte[] data, string openPhrase)
		{
			SetData(data, openPhrase);
		}

		protected void SetData(byte[] data, string openPhrase)
		{
			inMemory = CryptoServices.Encrypt(data, openPhrase);

			Debug.Assert(inMemory != null);
		}

		protected byte[] GetData(string openPhrase)
		{
			return CryptoServices.Decrypt(inMemory, openPhrase);
		}


		// SECURITY
		// on dispose, data gets overwritten and released to garbage collection
		public void Dispose()
		{
			if (inMemory != null)
			{
				Array.Copy(
					CryptoServices.CreateRandomBytes(inMemory.Length),
					inMemory,
					inMemory.Length);

				inMemory = null;
			}
		}
	}

}