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
		protected NetworkSimulation Simulation;
		private LocalStorageSimulation LocalStorage;

		protected Task SmallDelay
			=> Task.Delay(50);

		protected async Task WhenMessagesDelivered()
		{
			await Connection.WhenMessagesDelivered;
			await SmallDelay;
		}

		protected async Task ConnectToSimulation()
		{
			Engine = null;
			Connection = null;

			Trace.WriteLine("-- connect to simulation");

			var parameters = new SimulationParameters
			{
				AutoBlocks = false,
				CallLatency = TimeSpan.FromMilliseconds(50),
				EnforceValidation = false,
			};

			PrepareSimulation(parameters);

			await Engine.Start();

			Trace.WriteLine("--");
		}

		protected void PrepareSimulation(SimulationParameters parameters)
		{
			Simulation = new NetworkSimulation(parameters);

			Connection = Simulation.Connect("InstanceID");

			LocalStorage = new LocalStorageSimulation(parameters);

			Engine = new Engine(
				new EngineConfiguration
				{
					Connection = Connection,
					LocalStorage = LocalStorage,
				});
		}
	}
}