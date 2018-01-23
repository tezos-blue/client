﻿using System;

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

		// Copy constructor
		protected Secret(Secret other)
		{
			inMemory = (byte[])other.inMemory.Clone();
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
				CryptoServices.FillRandom(inMemory);

				inMemory = null;
			}
		}
	}

	public abstract class PhraseProtectedSecret : Secret
	{
		protected PhraseProtectedSecret(byte[] encryptedData)
		{
			Data = encryptedData;
		}

		protected PhraseProtectedSecret(byte[] data, Passphrase passphrase)
		{
			SetData(data, passphrase);
		}

		// Encrypt data with passphrase
		protected void SetData(byte[] data, Passphrase passphrase)
		{
			if (data == null) throw new ArgumentNullException(nameof(data));
			if (passphrase == null) throw new ArgumentNullException(nameof(passphrase));

			Data = passphrase.Encrypt(data);
		}

		// Decrypt data with passphrase
		protected byte[] GetData(Passphrase passphrase)
		{
			return passphrase.Decrypt(Data);
		}
	}
}