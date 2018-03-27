using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Protocol;
	using Simulation;
	using SLD.Tezos.Client.Model;
	using Tools;

	[TestClass]
	public class SimulationTest : ClientTest
	{
		private NetworkSimulation simulation;

		[TestInitialize]
		public async Task BeforeEach()
		{
			simulation = new NetworkSimulation();
		}

		[TestMethod]
		public async Task Simulation_Connect()
		{
			var endpoint = simulation.RegisterConnection("InstanceID");

			var monitor = new NetworkEventMonitor(endpoint);

			await simulation.Hub.Broadcast(new NetworkEvent());

			Assert.AreEqual(1, monitor.MessageCount);
		}

		[TestMethod]
		public async Task Simulation_RegisterIdentity()
		{
			var monitor = Connect();

			var task = new RegisterIdentityTask
			{
				IdentityID = "IdentityID",

				Client = new ClientInfo
				{
					InstanceID = "InstanceID"
				}
			};

			simulation.RegisterIdentity(task);

			Assert.AreEqual(0, monitor.MessageCount);

			await simulation.Hub.Notify("IdentityID", new NetworkEvent());

			Assert.AreEqual(1, monitor.MessageCount);
		}


		NetworkEventMonitor Connect()
		{
			var endpoint = simulation.RegisterConnection("InstanceID");

			return new NetworkEventMonitor(endpoint);
		}

		NetworkEventMonitor ConnectIdentity(string identityID)
		{
			var monitor = Connect();

			var task = new RegisterIdentityTask
			{
				IdentityID = identityID,

				Client = new ClientInfo
				{
					InstanceID = "InstanceID"
				}
			};

			simulation.RegisterIdentity(task);

			return monitor;
		}

	}
}