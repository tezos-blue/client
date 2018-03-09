using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Connections
{
	using OS;
	using Protocol;
	using Simulation;

	public class SimulatedConnection : TezosObject, IConnection
	{
		private ClientInfo clientInfo = new ClientInfo
		{
			InstanceID = LocalStorageSimulation.instanceID,
		};

		private NetworkSimulation simulation;

		private ConnectionEndpoint endpoint;

		public SimulatedConnection(SimulationParameters parameters = null)
		{
			simulation = new NetworkSimulation(parameters);

			if (parameters != null)
			{
				_ServiceState = parameters.ServiceState;
			}
		}

		public event Action<NetworkEvent> EventReceived;

		private string InstanceID => LocalStorageSimulation.instanceID;

		public async Task<ConnectionState> Connect(InstanceInfo registration)
		{
			await Task.Delay(50);

			endpoint = simulation.RegisterConnection(InstanceID);
			endpoint.EventFired += FireEventReceived;

			// TODO remove MonitoredAccounts from registration
			//await Monitor(registration.MonitoredAccounts);

			switch (ServiceState)
			{
				case ServiceState.Operational:
					return ConnectionState.Online;

				case ServiceState.Limited:
					return ConnectionState.Connected;

				case ServiceState.Down:
				default:
					throw new ApplicationException();
			}
		}

		public async Task Monitor(IEnumerable<string> accountIDs)
		{
			if (accountIDs != null)
			{
				await simulation.MonitorAccounts(InstanceID, accountIDs.ToArray());
			}
		}

		public void Disconnect()
		{
			simulation.UnregisterConnection(InstanceID);
			endpoint.EventFired -= FireEventReceived;
		}

		public async Task CreateBlock()
		{
			await simulation.CreateBlock();
		}

		public Task<AccountEntry[]> GetAccountEntries(string accountID)
		{
			return Task.FromResult(new AccountEntry[0]);
		}

		public async Task Timeout(ProtectedTask task)
		{
			await simulation.Timeout(task);
		}

		private void FireEventReceived(NetworkEvent networkEvent)
		{
			try
			{
				Trace($"NetworkEvent received: {networkEvent}");
				EventReceived?.Invoke(networkEvent);
			}
			catch (Exception e)
			{
				Trace(e);
			}
		}

		#region ServiceState

		private ServiceState _ServiceState = ServiceState.Operational;

		public ServiceState ServiceState
		{
			get
			{
				return _ServiceState;
			}

			set
			{
				if (_ServiceState != value)
				{
					_ServiceState = value;

					FireEventReceived(new ServiceEvent
					{
						ServiceState = value,
						EventID = ServiceEventID.ServiceStateChanged,
					});
				}
			}
		}

		#endregion ServiceState

		#region Operations

		public async Task<CreateFaucetTask> AlphaCreateFaucet(CreateFaucetTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.AlphaCreateFaucet(PrepareTask(task), InstanceID);
		}

		public async Task<CreateContractTask> PrepareCreateContract(CreateContractTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.PrepareCreateContract(PrepareTask(task));
		}

		public async Task<CreateContractTask> CreateContract(CreateContractTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.CreateContract(PrepareTask(task), InstanceID);
		}

		public async Task<TransferTask> PrepareTransfer(TransferTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.PrepareTransfer(PrepareTask(task));
		}

		public async Task<TransferTask> Transfer(TransferTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.CommitTransfer(PrepareTask(task));
		}

		#endregion Operations

		#region Identities

		public async Task<RegisterIdentityTask> RegisterIdentity(RegisterIdentityTask task)
		{
			await Latency();

			return await simulation.RegisterIdentity(PrepareTask(task));
		}

		#endregion Identities

		#region Accounts

		public async Task<decimal> GetBalance(string accountID)
		{
			await Latency();

			return await simulation.GetBalance(accountID);
		}

		public async Task<AccountInfo> GetAccountInfo(string accountID)
		{
			await Latency();

			return await simulation.GetAccountInfo(accountID);
		}

		public Task RemoveStaleAccount(string accountID, string managerID)
		{
			throw new NotImplementedException();
		}

		#endregion Accounts

		private T PrepareTask<T>(T task) where T : BaseTask
		{
			task.Client = clientInfo;

			return task;
		}

		private async Task Latency()
		{
			var latency = simulation.Parameters.CallLatency;

			if (latency.TotalMilliseconds > 0)
			{
				await Task.Delay(latency);
			}
		}
	}
}