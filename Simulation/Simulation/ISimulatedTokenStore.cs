using System.Collections.Generic;

namespace SLD.Tezos.Simulation
{
	using Protocol;
	using System.Threading.Tasks;

	interface ISimulatedTokenStore
	{
		List<ConnectionEndpoint> Listeners { get; }

		Task Notify(NetworkEvent netEvent);

		void SetBalance(decimal balance);
	}
}