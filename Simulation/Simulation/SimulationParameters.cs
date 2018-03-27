using System;

namespace SLD.Tezos.Simulation
{
	using Protocol;

	public class SimulationParameters
	{
		public TimeSpan TimeBetweenBlocks = TimeSpan.FromSeconds(60);
		public bool AutoBlocks { get; set; } = true;

		public bool EnforceValidation { get; set; } = true;

		public ServiceState ServiceState { get; set; } = ServiceState.Operational;

		public TimeSpan CallLatency { get; set; } = TimeSpan.FromSeconds(0.2);

		// internal use
		internal NetworkSimulation Simulation { get; set; }
	}
}