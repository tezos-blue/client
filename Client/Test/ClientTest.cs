using System;
using System.Diagnostics;
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

		protected Task WhenMessagesDelivered
			=> Connection.WhenMessagesDelivered;

		protected async Task ConnectToSimulation()
		{
			Engine = null;
			Connection = null;

			Trace.WriteLine("-- connect to simulation");

			var parameters = new SimulationParameters
			{
				AutoBlocks = false,
				CallLatency = TimeSpan.FromMilliseconds(50),
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