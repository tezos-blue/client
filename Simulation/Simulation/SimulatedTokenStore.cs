using System.Collections.Generic;
using System.Threading.Tasks;

namespace SLD.Tezos.Simulation
{
	using Protocol;

	public abstract class SimulatedTokenStore
	{
		public decimal Balance;
		public string Name;
		public string AccountID;
		public string Stereotype;

		internal SimulatedIdentity Manager;
		internal HashSet<ConnectionEndpoint> Listeners = new HashSet<ConnectionEndpoint>();
		internal List<AccountEntry> Entries = new List<AccountEntry>();

		private NetworkSimulation simulation;

		public SimulatedTokenStore(NetworkSimulation simulation)
		{
			this.simulation = simulation;
		}

		public abstract bool IsLive { get; }
		public string ManagerID => Manager.AccountID;

		public Task Notify(NetworkEvent netEvent)
			=> simulation.Hub.Notify(this, netEvent);
	}
}