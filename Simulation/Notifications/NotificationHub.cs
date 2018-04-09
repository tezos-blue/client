using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SLD.Tezos.Notifications
{
	using Protocol;
	using Simulation;

	public class NotificationHub : SimulationObject
	{
		private Dictionary<string, ConnectionEndpoint> connections = new Dictionary<string, ConnectionEndpoint>();

		private int pendingCount;

		private SyncEvent syncPendingSent;

		public NotificationHub(SimulationParameters parameters) : base(parameters)
		{
			// start with all messages sent
			syncPendingSent = new SyncEvent();
			syncPendingSent.SetComplete();
		}

		public Task WhenPendingSent
		{
			get
			{
				lock (this)
				{
					return syncPendingSent.WhenComplete;
				}
			}
		}

		public async Task Broadcast(NetworkEvent networkEvent)
		{
			foreach (var endpoint in connections.Values)
			{
				Notify(endpoint, networkEvent);
			}

			await WhenPendingSent;
		}

		public async Task Notify(string accountID, NetworkEvent networkEvent)
		{
			var eventSource = Parameters.Simulation.GetAccount(accountID) as SimulatedTokenStore;

			await Notify(eventSource, networkEvent);
		}

		internal ConnectionEndpoint Register(string instanceID)
		{
			Debug.Assert(!connections.ContainsKey(instanceID));

			var endpoint = new ConnectionEndpoint
			{
				ID = instanceID,
			};

			connections.Add(instanceID, endpoint);

			return endpoint;
		}

		internal void Unregister(string instanceID)
		{
			connections.Remove(instanceID);
		}

		internal void MonitorAccount(string instanceID, SimulatedTokenStore account)
		{
			var connection = connections[instanceID];
			account.Listeners.Add(connection);
		}

		internal async Task Notify(SimulatedTokenStore eventSource, NetworkEvent networkEvent)
		{
			Trace($"Notify {eventSource} | {networkEvent}");

			foreach (var endpoint in eventSource.Listeners)
			{
				Notify(endpoint, networkEvent);
			}

			await WhenPendingSent;
		}

		private async void Notify(ConnectionEndpoint endpoint, NetworkEvent networkEvent)
		{
			lock (this)
			{
				pendingCount++;

				if (pendingCount == 1)
				{
					Trace("Queue started");
					syncPendingSent = new SyncEvent();
				}
			}

			await Task.Delay(Parameters.CallLatency);

			endpoint.Notify(networkEvent);

			lock (this)
			{
				pendingCount--;

				if (pendingCount == 0)
				{
					Trace("Queue empty");
					syncPendingSent.SetComplete();
				}
			}
		}
	}
}