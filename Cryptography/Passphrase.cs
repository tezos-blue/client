using System.Text;

namespace SLD.Tezos.Cryptography
{
	/// <summary>
	/// Text-based key to a PhraseProtectedSecret
	/// </summary>
	/// <seealso cref="PhraseProtectedSecret" />
	public sealed class Passphrase : Secret
	{
		private static readonly byte[] Salt = { 5, 7, 8, 2, 8, 23, 5, 3, 45, 2, 23, 4, 4, 3, 5 };

		public Passphrase(string passphrase)
		{
			// Scramble string
			Data = Encoding.UTF8.GetBytes(passphrase);
		}

		// Copy constructor
		public Passphrase(Passphrase passphrase) : base(passphrase) { }

		// Conveniently convert string to Passphrase where required
		public static implicit operator Passphrase(string passphrase)
			=> passphrase != null ? new Passphrase(passphrase) : null;

		// SECURITY
		// Access to the data is restricted to the Cryptography assembly
		// Passphrase has no public properties, the clear text can not be recovered in code
		internal byte[] Encrypt(byte[] data)
		{
			using (var bytes = CryptoServices.DeriveBytes(Data, Salt))
			{
				return CryptoServices.Encrypt(data, bytes);
			}
		}

		internal byte[] Decrypt(byte[] data)
		{
			using (var bytes = CryptoServices.DeriveBytes(Data, Salt))
			{
				return CryptoServices.Decrypt(data, bytes);
			}
		}
	}
}