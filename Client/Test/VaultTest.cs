using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Model;
	using OS;
	using Security;

	[TestClass]
	public class VaultTest
	{
		[TestMethod]
		public void Vault_SerializeSlot()
		{
			// Create slot
			var original = new SoftwareVault.Slot
			{
				Name = "Name",
				Stereotype = "Stereotype",
				Keys = new KeyPair
				{
					PublicKey = new PublicKey(new byte[] { 1, 2, 3 }),
					PrivateKey = new PrivateKey(new byte[] { 4, 5, 6 }, "Passphrase"),
				}
			};

			// Serialize - Deserialize
			var stream = new MemoryStream();
			var formatter = new BinaryFormatter();

			formatter.Serialize(stream, original);

			stream.Position = 0;

			var restored = (SoftwareVault.Slot)formatter.Deserialize(stream);

			// Assert equality
			Assert.AreEqual(original.Name, restored.Name);
			Assert.AreEqual(original.Stereotype, restored.Stereotype);

			Assert.AreEqual(
				original.Keys.PublicID,
				restored.Keys.PublicID);

			CollectionAssert.AreEqual(
				original.Keys.PublicKey.Data,
				restored.Keys.PublicKey.Data);

			CollectionAssert.AreEqual(
				original.Keys.PrivateKey.EncryptedData,
				restored.Keys.PrivateKey.EncryptedData
				);
		}

		[TestMethod]
		public async Task Vault_StoreIdentity()
		{
			var storage = new InMemoryStore();

			var vault = new SoftwareVault(storage);
			await vault.Initialize();

			// Create and store identity
			var identity = await vault.CreateIdentity("Identity", "Passphrase", "Stereotype");

			// Reopen Vault
			vault = new SoftwareVault(storage);
			await vault.Initialize();

			var storedIdentities = vault.IdentityIDs;

			Assert.IsNotNull(storedIdentities);
			Assert.AreEqual(1, storedIdentities.Count());

			var storedID = storedIdentities.First();

			Assert.AreEqual(identity.AccountID, storedID);

			// Identities will be created via IProvideSigning
			var restored = new Identity(
				new PublicKey(vault.GetPublicKey(storedID)),
				vault);

			Assert.IsNotNull(restored);

			Assert.AreEqual(identity.Name, restored.Name);
			Assert.AreEqual(identity.Stereotype, restored.Stereotype);
			Assert.AreEqual(identity.PublicKey, restored.PublicKey);
		}
	}

	internal class InMemoryStore : IStoreLocal
	{
		private Dictionary<string, MemoryStream> files = new Dictionary<string, MemoryStream>();

		public Task<IEnumerable<Stream>> OpenIdentityFilesAsync()
		{
			var streams = files
				.Select(file => new MemoryStream(file.Value.ToArray()));

			return Task.FromResult(streams.Cast<Stream>());
		}

		public Task<Stream> CreateIdentityFileAsync(string accountID)
		{
			var stream = new MemoryStream();

			files.Add(accountID, stream);

			return Task.FromResult(stream as Stream);
		}

		public Task DeleteIdentity(string identityID)
		{
			files.Remove(identityID);

			return Task.CompletedTask;
		}

		public Task PurgeAll()
		{
			files.Clear();

			return Task.CompletedTask;
		}
	}
}