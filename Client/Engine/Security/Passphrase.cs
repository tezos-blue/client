using System.Security.Cryptography;
using System.Text;

namespace SLD.Tezos.Client.Security
{
	using Cryptography;

	internal class Passphrase : Secret
	{
		private static readonly byte[] Salt = { 1, 2, 3, 4 };
		private string passphrase;

		internal Passphrase(string passphrase)
		{
			Data = Encoding.UTF8.GetBytes(passphrase);
		}

		public PasswordDeriveBytes Bytes => new PasswordDeriveBytes(Data, Salt);
	}
}