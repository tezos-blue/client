using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Cryptography;
	using Protocol;

	internal class HardwareWallet : IProvideSigning
	{
		// the only identity on this wallet
		private byte[] publicKey;

		private byte[] privateKey;

		private string identityID;

		public IEnumerable<string> IdentityIDs => new string[]
		{
			identityID
		};

		public Task Initialize()
		{
			CryptoServices.CreateKeyPair(out publicKey, out privateKey);

			identityID = CryptoServices.CreatePrefixedHash(HashType.PublicKeyHash, publicKey);

			return Task.CompletedTask;
		}

		public Task<bool> Sign(byte[] data, string identityID, out byte[] signature)
		{
			Assert.AreEqual(this.identityID, identityID);

			signature = CryptoServices.CreateSignature(privateKey, data);

			return Task.FromResult(true);
		}

		public bool Contains(string identityID) => identityID == this.identityID;

		public byte[] GetPublicKey(string identityID)
		{
			Assert.AreEqual(this.identityID, identityID);

			return publicKey;
		}
	}

	partial class SecurityTest
	{
		[TestMethod]
		public async Task Security_GenericSign()
		{
			// Register the hardware with the Engine
			var signer = new HardwareWallet();
			await Engine.Register(signer);

			// Now we can access the identity in the hardware
			var identity = Engine.Identities.First();

			// Create, sign and submit a new contract origination
			var task = await Engine.CreateAccount("Test", identity, identity, 1000);

			// It should have been accepted by the network
			Assert.AreEqual(TaskProgress.Submitted, task.Progress);
		}
	}
}