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

		public SimulatedIdentity() : base("")
		{
		}

		public SimulatedIdentity(string accountID, IEnumerable<Account> accounts = null) : this()
		{
			simAccountID = accountID;

			if (accounts != null)
			{
				foreach (var account in accounts)
				{
					AddAccount(account);
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

		#region Serialization

		public SimulatedIdentity(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			simAccountID = info.GetString("SimAccountID");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("SimAccountID", simAccountID);
		}

		#endregion Serialization
	}
}