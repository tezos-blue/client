﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Connections
{
	using Protocol;
	using Simulation;

	public class SimulatedConnection : TezosObject, IConnection
	{
		public string InstanceID;
		private NetworkSimulation simulation;

		private ConnectionEndpoint endpoint;

		internal SimulatedConnection(SimulationParameters parameters)
		{
			simulation = parameters.Simulation;

			_ServiceState = parameters.ServiceState;
		}

		public async Task<ConnectionState> Connect(InstanceInfo registration)
		{
			await CallService();

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
			await CallService();

			if (accountIDs != null)
			{
				simulation.MonitorAccounts(InstanceID, accountIDs.ToArray());
			}
		}

		public void Disconnect()
		{
			simulation.UnregisterConnection(InstanceID);
			endpoint.EventFired -= FireEventReceived;
		}

		public async Task<AccountEntry[]> GetAccountEntries(string accountID)
		{
			await CallService();

			var account = simulation.GetAccount(accountID);

			return account.Entries
				.ToArray();
		}

		#region Events

		public event Action<NetworkEvent> EventReceived;

		public Task WhenMessagesDelivered
			=> simulation.Hub.WhenPendingSent;

		public bool IsPushEnabled { get; set; } = true;

		public void FireEventReceived(NetworkEvent networkEvent)
		{
			if (!IsPushEnabled) return;

			try
			{
				Trace($"{networkEvent.GetType().Name} received: {networkEvent}");
				EventReceived?.Invoke(networkEvent);
			}
			catch (Exception e)
			{
				Trace(e);
			}
		}

		#endregion Events

		#region Failure

		private int callsUntilFailure = -1;
		public bool IsOnline { get; set; } = true;

		public void FailAfter(int callsUntilFailure)
		{
			this.callsUntilFailure = callsUntilFailure;
		}

		private void CheckFailure()
		{
			if (!IsOnline)
			{
				throw new HttpRequestException("Not online");
			}

			if (callsUntilFailure >= 0)
			{
				callsUntilFailure--;

				if (callsUntilFailure == -1)
				{
					throw new HttpRequestException("Call failed");
				}
			}
		}

		#endregion Failure

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

		public async Task<ActivateIdentityTask> ActivateIdentity(ActivateIdentityTask task)
		{
			await CallService();

			return simulation.ActivateIdentity(PrepareTask(task), InstanceID);
		}

		public async Task<CreateContractTask> PrepareCreateContract(CreateContractTask task)
		{
			await CallService();

			return simulation.PrepareCreateContract(PrepareTask(task));
		}

		public async Task<CreateContractTask> CreateContract(CreateContractTask task)
		{
			await CallService();

			return simulation.CreateContract(PrepareTask(task), InstanceID);
		}

		public async Task<TransferTask> PrepareTransfer(TransferTask task)
		{
			await CallService();

			return simulation.PrepareTransfer(PrepareTask(task));
		}

		public async Task<TransferTask> Transfer(TransferTask task)
		{
			await CallService();

			return simulation.CommitTransfer(PrepareTask(task));
		}

		public async Task<OperationStatus> GetOperationStatus(OperationTask task)
		{
			await CallService();

			return simulation.GetOperationStatus(task);
		}

		#endregion Operations

		#region Identities

		public async Task<RegisterIdentityTask> RegisterIdentity(RegisterIdentityTask task)
		{
			await CallService();

			return simulation.RegisterIdentity(PrepareTask(task));
		}

		public async Task<IdentityInfo> GetIdentityInfo(string identityID)
		{
			await CallService();

			return simulation.GetIdentityInfo(identityID);
		}

		#endregion Identities

		#region Accounts

		public async Task<decimal> GetBalance(string accountID)
		{
			await CallService();

			return simulation.GetBalance(accountID);
		}

		public async Task<AccountInfo> GetAccountInfo(string accountID)
		{
			await CallService();

			return simulation.GetAccountInfo(accountID);
		}

		public async Task RemoveStaleAccount(string accountID, string managerID)
		{
			await CallService();

			simulation.RemoveStaleAccount(accountID, managerID);
		}

		#endregion Accounts

		private T PrepareTask<T>(T task) where T : BaseTask
		{
			task.Client = new ClientInfo
			{
				InstanceID = InstanceID,
			};

			return task;
		}

		private async Task CallService()
		{
			CheckFailure();

			var latency = simulation.Parameters.CallLatency;

			if (latency.TotalMilliseconds > 0)
			{
				await Task.Delay(latency);
			}
		}
	}
}