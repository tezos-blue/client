namespace SLD.Tezos
{
	using Simulation;

	public class SimulationObject : TezosObject
	{
		public SimulationObject(SimulationParameters parameters)
		{
			Parameters = parameters ?? new SimulationParameters();
		}

		public SimulationParameters Parameters { get; private set; }
	}
}