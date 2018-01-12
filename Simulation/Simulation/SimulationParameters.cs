using System;
using System.Collections.Generic;
using System.Text;

namespace SLD.Tezos.Simulation
{
    public class SimulationParameters
    {
		public bool AutoBlocks { get; set; } = true;

		public bool StartFresh { get; set; } = true;
		public bool AddStaleAccount { get; set; } = false;

		public NetworkSimulation Simulation { get; set; }

		public TimeSpan CallLatency { get; set; } = TimeSpan.FromSeconds(1);

		public TimeSpan TimeBetweenBlocks = TimeSpan.FromSeconds(60);

	}
}
