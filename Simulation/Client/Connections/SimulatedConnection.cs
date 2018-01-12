using System.Collections.Generic;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Connections
{
	using System;
	using OS;
	using Protocol;
	using Simulation;

	public class SimulatedConnection : Connection
	{
		private NetworkSimulation simulation;
		private ConnectionEndpoint endpoint;

		public SimulatedConnection(SimulationParameters parameters = null)
		{
			simulation = new NetworkSimulation(parameters);
		}

		private string InstanceID => LocalStorageSimulation.instanceID;

		public async override Task Connect(InstanceInfo registration)
		{
			await Task.Delay(50);
			endpoint = simulation.RegisterConnection(InstanceID);
			endpoint.EventFired += FireEventReceived;

			await Monitor(registration.MonitoredAccounts);
		}

		public async override Task Monitor(IEnumerable<string> accountIDs)
		{
			await simulation.MonitorAccounts(InstanceID, accountIDs);
		}

		public override void Disconnect()
		{
			simulation.UnregisterConnection(InstanceID);
			endpoint.EventFired -= FireEventReceived;
		}

		#region Operations

		public override async Task<CreateFaucetTask> AlphaCreateFaucet(CreateFaucetTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.AlphaCreateFaucet(task, InstanceID);
		}

		public override async Task<CreateContractTask> PrepareCreateContract(CreateContractTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.PrepareCreateContract(task);
		}

		public async override Task<CreateContractTask> CreateContract(CreateContractTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.CreateContract(task, InstanceID);
		}

		public override async Task<TransferTask> PrepareTransfer(TransferTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.PrepareTransfer(task);
		}

		public override async Task<TransferTask> Transfer(TransferTask task)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

			return await simulation.CommitTransfer(task);
		}

		#endregion Operations

		#region Accounts

		public override async Task<decimal> GetBalance(string accountID)
		{
			await Latency();

			return await simulation.GetBalance(accountID);
		}

		public override async Task<AccountInfo> GetAccountInfo(string accountID)
		{
			await Task.Delay(simulation.Parameters.CallLatency);

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

		public async Task CreateBlock()
		{
			await simulation.CreateBlock();
		}

		public override Task<AccountEntry[]> GetAccountEntries(string accountID)
		{
			throw new System.NotImplementedException();
		}

		public async Task Timeout(ProtectedTask task)
		{
			await simulation.Timeout(task);
		}
	}
}