using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SLD.Tezos.Client.Model
{
	using Protocol;
	using Simulation;
	using System.Threading.Tasks;

	[Serializable]
	public class SimulatedAccount : Account, ISimulatedTokenStore
	{
		private static int NextID = 1;

		NetworkSimulation simulation;

		public SimulatedAccount(NetworkSimulation simulation, SimulatedIdentity manager, decimal balance) : base(null, $"Account {NextID++}")
		{
			this.simulation = simulation;

			UpdateBalance(balance);
			Name = AccountID;

			SetManager(manager);
		}

		//public SimulatedAccount(SerializationInfo info, StreamingContext context) : base(info, context)
		//{
		//}

		public List<ConnectionEndpoint> Listeners { get; private set; } = new List<ConnectionEndpoint>();

		public Task Notify(NetworkEvent netEvent)
			=> simulation.Hub.Notify(this, netEvent);

		public void SetBalance(decimal balance)
		{
			Balance = balance;
		}
	}
}