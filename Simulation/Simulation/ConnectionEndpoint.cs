using System;
using System.Collections.Generic;

namespace SLD.Tezos.Simulation
{
	using Protocol;

	public class ConnectionEndpoint
	{
		public List<string> MonitoredAccountIDs = new List<string>();

		public event Action<NetworkEvent> EventFired;

		public string ID { get; set; }

		internal void Notify(NetworkEvent netEvent)
		{
			EventFired?.Invoke(netEvent);
		}
	}
}