using System.Collections.Generic;

namespace SLD.Tezos.Simulation
{
	using Protocol;

	internal interface IEventSource
	{
		List<ConnectionEndpoint> Listeners { get; }

		void Notify(NetworkEvent netEvent);
	}
}