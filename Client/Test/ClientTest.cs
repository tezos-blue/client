using System;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Connections;
	using OS;
	using Simulation;

	public class ClientTest
	{
		protected WalletEngine Engine;
		protected SimulatedConnection Connection;

		protected async Task ConnectToSimulation()
		{
			var parameters = new SimulationParameters
			{
				AutoBlocks = false,
				CallLatency = TimeSpan.FromSeconds(0),
			};

			Connection = new SimulatedConnection(parameters);

			Engine = new WalletEngine(
				new WalletEngineConfiguration
				{
					Connection = Connection,
					OSLocalStorage = new LocalStorageSimulation(parameters),
				});

			await Engine.Start();
		}
	}
}