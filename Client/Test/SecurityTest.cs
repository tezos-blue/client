using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Cryptography;

	[TestClass]
	public partial class SecurityTest : ClientTest
	{
		[TestInitialize]
		public async Task BeforeEach()
		{
			await ConnectToSimulation();
		}

		// SECURITY
		// Secrets are scrambled while in memory
		[TestMethod]
		public void Security_SecretCanBeRetrieved()
		{
			var secretData = new byte[] { 1, 2, 3, 4, 5 };

			var secret = new TestSecret
			{
				OpenData = secretData
			};

			CollectionAssert.AreEqual(secretData, secret.OpenData);
		}

		// SECURITY
		// A user must pass in a passphrase when creating a new identity.
		// But: An empty passphrase is accepted as the user's own choice and responsibility
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public async Task Security_MustSetPassword()
		{
			await Engine.AddIdentity("Identity", null);
		}

		// SECURITY
		// Unless explicitly stated, a new identity is initially locked
		[TestMethod]
		public async Task Security_NewIdentityLocked()
		{
			var identity = await Engine.AddIdentity("Identity", "My passphrase");

			Assert.IsTrue(identity.IsLocked);

			var unlocked = identity.Unlock("My passphrase");
			Assert.IsTrue(unlocked);
			Assert.IsFalse(identity.IsLocked);
		}

		// SECURITY
		// Wrong passphrases are detected, brute force is hindered
		[TestMethod]
		public async Task Security_WrongPassphrase()
		{
			var identity = await Engine.AddIdentity("Identity", "My passphrase");

			var unlocked = identity.Unlock("Wrong passphrase");
			Assert.IsFalse(unlocked);
			Assert.IsTrue(identity.IsLocked);
		}

		// SECURITY
		// A new identity can be explicitly unlocked on creation
		[TestMethod]
		public async Task Security_NewIdentityKeepUnlocked()
		{
			var identity = await Engine.AddIdentity("Identity", "My passphrase", true);

			Assert.IsTrue(identity.IsUnlocked);
		}

		// SECURITY
		// All identities are locked on pause/resume events
		[TestMethod]
		public async Task Security_LockOnPauseResume()
		{
			var identity = await Engine.AddIdentity("Identity", "My passphrase", true);

			Engine.Suspend();

			Assert.IsTrue(identity.IsLocked);

			identity.Unlock("My passphrase");

			await Engine.Resume();

			Assert.IsTrue(identity.IsLocked);
		}
	}

	internal class TestSecret : Secret
	{
		public byte[] OpenData
		{
			get => Data;

			set => Data = value;
		}
	}
}