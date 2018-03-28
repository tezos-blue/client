using System;
using System.Collections.Generic;
using System.Linq;

namespace SLD.Tezos.Simulation
{
	[Serializable]
	public class SimulatedIdentity : SimulatedTokenStore
	{
		public List<SimulatedTokenStore> Accounts = new List<SimulatedTokenStore>();

		public SimulatedIdentity(NetworkSimulation simulation, string accountID) : base(simulation)
		{
			Name = AccountID = accountID;

			Manager = this;
		}

		internal IEnumerable<SimulatedAccount> ManagedAccounts
			=> Accounts
			.Where(a => a.AccountID != AccountID)
			.Cast<SimulatedAccount>();

		public override bool IsLive => true;
	}
}