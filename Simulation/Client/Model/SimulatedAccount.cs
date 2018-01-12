using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SLD.Tezos.Client.Model
{
	using Protocol;
	using Simulation;

	[Serializable]
	public class SimulatedAccount : Account, IEventSource
	{
		private static int NextID = 1;

		public SimulatedAccount(SimulatedIdentity manager, decimal balance) : base(null, $"Account {NextID++}")
		{
			UpdateBalance(balance);
			Name = AccountID;

			SetManager(manager);
		}

		public SimulatedAccount(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

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