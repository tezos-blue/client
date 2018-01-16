using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Cryptography;

	[TestClass]
	public class SecurityTest : ClientTest
	{
		[TestInitialize]
		public async Task BeforeEach()
		{
			await ConnectToSimulation();
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
		// Secrets are scrambled while in memory and must be 
		// But: An empty passphrase is accepted as the user's own choice and responsibility

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