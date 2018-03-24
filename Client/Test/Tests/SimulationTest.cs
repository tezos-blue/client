using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Protocol;
	using Simulation;

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
			List<NetworkEvent> received = new List<NetworkEvent>();

			var endpoint = simulation.RegisterConnection("InstanceID");

			endpoint.EventFired += (networkEvent) =>
			{
				received.Add(networkEvent);
			};

			simulation.Hub.Broadcast(new NetworkEvent());

			await simulation.Hub.WhenPendingSent;

			Assert.AreEqual(1, received.Count);
		}
	}
}