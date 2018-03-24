using System;

namespace SLD.Tezos.Simulation
{
	using Protocol;

	public class ConnectionEndpoint
	{
		public event Action<NetworkEvent> EventFired;

		public string ID { get; set; }

		internal void Notify(NetworkEvent netEvent)
		{
			EventFired?.Invoke(netEvent);
		}
	}
}