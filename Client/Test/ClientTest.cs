using System;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Connections;
	using OS;
	using Simulation;
	using System.Diagnostics;

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

			Connection = new SimulatedConnection(parameters);

			Engine = new Engine(
				new EngineConfiguration
				{
					Connection = Connection,
					LocalStorage = new LocalStorageSimulation(parameters),
				});

			await Engine.Start();

			Trace.WriteLine("--");
		}
	}
}