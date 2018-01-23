using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SLD.Tezos.Client.Model
{
	using Cryptography;
	using Protocol;
	using Security;
	using Simulation;

	[Serializable]
	public class SimulatedIdentity : Identity, IEventSource
	{
		private string simAccountID;

		public SimulatedIdentity()
		{
			CryptoServices.CreateKeyPair(out byte[] publicKey, out byte[] privateKey);

			PublicKey = new PublicKey(publicKey);
		}

		public SimulatedIdentity(string accountID, IEnumerable<Account> accounts = null) : this()
		{
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

		public void Notify(NetworkEvent netEvent)
		{
			foreach (var listener in Listeners)
			{
				listener.Notify(netEvent);
			}
		}
	}
}