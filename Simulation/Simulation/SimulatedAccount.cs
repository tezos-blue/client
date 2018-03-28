using System;

namespace SLD.Tezos.Simulation
{
	[Serializable]
	public class SimulatedAccount : SimulatedTokenStore
	{
		private static int NextID = 1;

		public SimulatedAccount(NetworkSimulation simulation, SimulatedIdentity manager, decimal balance) : base(simulation)
		{
			Balance = balance;
			Name = AccountID = $"Account {NextID++}";

			Manager = manager;
		}

		public override bool IsLive => Balance >= NetworkSimulation.MinimumBalance;
	}
}