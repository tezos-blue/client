using System.Security.Cryptography;
using System.Text;

namespace SLD.Tezos.Cryptography
{
	public class Passphrase : Secret
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