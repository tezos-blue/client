using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace SLD.Tezos.Client.Tools
{
	using Protocol;
	using Simulation;

	internal class NetworkEventMonitor
	{
		private ConnectionEndpoint endpoint;
		private List<NetworkEvent> received = new List<NetworkEvent>();

		public NetworkEventMonitor(ConnectionEndpoint endpoint)
		{
			this.endpoint = endpoint;

			endpoint.EventFired += (networkEvent) =>
			{
				received.Add(networkEvent);
			};
		}

		public int MessageCount => received.Count;

		public T Single<T>() where T : NetworkEvent
			=> received.Single(message => message.GetType() == typeof(T)) as T;

		internal void AssertCount(int count)
			=> Assert.AreEqual(count, MessageCount);

		internal void Clear()
			=> received.Clear();
	}
}