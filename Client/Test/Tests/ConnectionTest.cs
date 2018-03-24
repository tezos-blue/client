using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SLD.Tezos.Client
{
	using Connections;
	using Simulation;
	using Protocol;

	[TestClass]
	public class ConnectionTest : ClientTest
	{
		[TestMethod]
		public async Task Connect_EngineOperational()
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

		[TestMethod]
		public async Task Connect_EngineLimited()
		{
			var parameters = new SimulationParameters
			{
				AutoBlocks = false,
				CallLatency = TimeSpan.FromSeconds(0.1),
				ServiceState = ServiceState.Limited,
			};

			PrepareSimulation(parameters);

			await Engine.Start();

			Assert.IsTrue(Engine.IsConnected);
			Assert.AreEqual(ConnectionState.Connected, Engine.ConnectionState);
		}

		[TestMethod]
		public async Task Connect_EngineDown()
		{
			var parameters = new SimulationParameters
			{
				AutoBlocks = false,
				CallLatency = TimeSpan.FromSeconds(0.1),
				ServiceState = ServiceState.Down,
			};

			PrepareSimulation(parameters);

			var taskStart = Engine.Start();

			await Task.Delay(200);

			Assert.IsFalse(Engine.IsConnected);
			Assert.AreEqual(ConnectionState.Disconnected, Engine.ConnectionState);
		}

	}
}
