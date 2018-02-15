using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SLD.Tezos.Client
{
	using Connections;
	using Simulation;

	[TestClass]
	public class ConnectionTest : ClientTest
	{
		[TestMethod]
		public async Task Connect_EngineComplete()
		{
			var parameters = new SimulationParameters
			{
				AutoBlocks = false,
				CallLatency = TimeSpan.FromSeconds(0.1),
			};

			PrepareSimulation(parameters);

			Assert.IsFalse(Engine.IsConnected);
			Assert.AreEqual(ConnectionState.Disconnected, Engine.ConnectionState);

			var startTask = Engine.Start();
			Assert.AreEqual(ConnectionState.Connecting, Engine.ConnectionState);

			await startTask;

			Assert.IsTrue(Engine.IsConnected);
			Assert.AreEqual(ConnectionState.Online, Engine.ConnectionState);

		}
	}
}
