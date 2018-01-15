using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SLD.Tezos.Client
{
	using Simulation;
	using Connections;
	using OS;

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
	}
}
