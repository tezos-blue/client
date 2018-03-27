using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLD.Tezos.Simulation;

namespace SLD.Tezos.Client.Tools
{
	using Protocol;

	class NetworkEventMonitor
	{
		private ConnectionEndpoint endpoint;
		List<NetworkEvent> received = new List<NetworkEvent>();


		public NetworkEventMonitor(ConnectionEndpoint endpoint)
		{
			this.endpoint = endpoint;

			endpoint.EventFired += (networkEvent) =>
			{
				received.Add(networkEvent);
			};
		}

		public int MessageCount => received.Count;
	}
}
