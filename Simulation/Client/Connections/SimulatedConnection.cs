using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Connections
{
	using OS;
	using Protocol;
	using Simulation;

	public class SimulatedConnection : TezosObject, IConnection
	{
		private NetworkSimulation simulation;

		private ConnectionEndpoint endpoint;

		public SimulatedConnection(SimulationParameters parameters = null)
		{
			simulation = new NetworkSimulation(parameters);
		}

		public event Action<NetworkEvent> EventReceived;

		private string InstanceID => LocalStorageSimulation.instanceID;

		public async Task Connect(InstanceInfo registration)
		{
			await Task.Delay(50);
			endpoint = simulation.RegisterConnection(InstanceID);
			endpoint.EventFired += FireEventReceived;

			await Monitor(registration.MonitoredAccounts);
		}

		public async Task Monitor(IEnumerable<string> accountIDs)
		{
			await simulation.MonitorAccounts(InstanceID, accountIDs);
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
			throw new System.NotImplementedException();
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

		#region Operations

		public async Task<CreateFaucetTask> AlphaCreateFaucet(CreateFaucetTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.AlphaCreateFaucet(task, InstanceID);
		}

		public async Task<CreateContractTask> PrepareCreateContract(CreateContractTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.PrepareCreateContract(task);
		}

		public async Task<CreateContractTask> CreateContract(CreateContractTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.CreateContract(task, InstanceID);
		}

		public async Task<TransferTask> PrepareTransfer(TransferTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.PrepareTransfer(task);
		}

		public async Task<TransferTask> Transfer(TransferTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.CommitTransfer(task);
		}

		#endregion Operations

		#region Accounts

		public async Task<IdentityInfo> GetIdentityInfo(string identityID)
		{
			await Latency();

			return await simulation.GetIdentityInfo(identityID);
		}

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

		#endregion Accounts

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