using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Model
{
	using Cryptography;
	using Protocol;
	using Security;
	using Simulation;

	[Serializable]
	public class SimulatedIdentity : Identity, ISimulatedTokenStore
	{
		private string simAccountID;
		NetworkSimulation simulation;

		public SimulatedIdentity()
		{
			CryptoServices.CreateKeyPair(out byte[] publicKey, out byte[] privateKey);

			PublicKey = new PublicKey(publicKey);
		}

		public SimulatedIdentity(NetworkSimulation simulation, string accountID, IEnumerable<Account> accounts = null) : this()
		{
			this.simulation = simulation;

			simAccountID = accountID;

			if (accounts != null)
			{
				foreach (var account in accounts)
				{
					var task = AddAccount(account);
				}
			}
		}

		public override string AccountID => simAccountID;

		public List<ConnectionEndpoint> Listeners { get; private set; } = new List<ConnectionEndpoint>();

		internal IEnumerable<SimulatedAccount> ManagedAccounts
			=> Accounts
			.Where(a => a.AccountID != AccountID)
			.Cast<SimulatedAccount>();

		public Task Notify(NetworkEvent netEvent)
			=> simulation.Hub.Notify(this, netEvent);

		public void SetBalance(decimal balance)
		{
			Balance = balance;
		}
	}
}