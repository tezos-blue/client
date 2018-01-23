using System.Security.Cryptography;
using System.Text;

namespace SLD.Tezos.Cryptography
{
	/// <summary>
	/// Text-based key to a PhraseProtectedSecret
	/// </summary>
	/// <seealso cref="PhraseProtectedSecret" />
	public sealed class Passphrase : Secret
	{
		private static readonly byte[] Salt = { 1, 2, 3, 4 };

		public Passphrase(string passphrase)
		{
			// Scramble string
			Data = Encoding.UTF8.GetBytes(passphrase);
		}

		// Copy constructor
		public Passphrase(Passphrase passphrase) : base(passphrase) { }

		// Conveniently convert string to Passphrase where required
		public static implicit operator Passphrase(string passphrase) => new Passphrase(passphrase);

		// SECURITY
		// Access to the data is restricted to the Cryptography assembly
		// Passphrase has no public properties, the clear text can not be recovered in code
		internal byte[] Encrypt(byte[] data)
		{
			using (var bytes = new PasswordDeriveBytes(Data, Salt))
			{
				return CryptoServices.Encrypt(data, bytes);
			}
		}

		internal byte[] Decrypt(byte[] data)
		{
			using (var bytes = new PasswordDeriveBytes(Data, Salt))
			{
				return CryptoServices.Decrypt(data, bytes);
			}
		}
	}
}