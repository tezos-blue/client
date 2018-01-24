using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SLD.Tezos.Client
{
	using Security;
	using OS;

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
	}
}