namespace SLD.Tezos.Cryptography
{
	// SECURITY
	// All secrets are scrambled while in memory
	public abstract class Secret
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
	}
}