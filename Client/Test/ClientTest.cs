using System;
using System.Threading.Tasks;
using System.Diagnostics;

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
			Engine = null;
			Connection = null;

			Trace.WriteLine("-- connect to simulation");

			var parameters = new SimulationParameters
			{
				AutoBlocks = false,
				CallLatency = TimeSpan.FromSeconds(0),
			};

			PrepareSimulation(parameters);

			await Engine.Start();

			Trace.WriteLine("--");
		}

		protected void PrepareSimulation(SimulationParameters parameters)
		{
			Connection = new SimulatedConnection(parameters);

			Engine = new Engine(
				new EngineConfiguration
				{
					Connection = Connection,
					LocalStorage = new LocalStorageSimulation(parameters),
				});
		}
	}
}