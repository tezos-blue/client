using System;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Connections;
	using OS;
	using Simulation;

	public class ClientTest
	{
		protected Engine Engine;
		protected SimulatedConnection Connection;

		protected async Task ConnectToSimulation()
		{
			var parameters = new SimulationParameters
			{
				AutoBlocks = false,
				CallLatency = TimeSpan.FromSeconds(0),
			};

			Connection = new SimulatedConnection(parameters);

			Engine = new Engine(
				new EngineConfiguration
				{
					Connection = Connection,
					LocalStorage = new LocalStorageSimulation(parameters),
				});

			await Engine.Start();
		}
	}
}