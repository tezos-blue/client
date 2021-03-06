﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Connections
{
	using Protocol;

	public interface IConnection
	{
		event Action<NetworkEvent> EventReceived;

		#region Identities

		Task<RegisterIdentityTask> RegisterIdentity(RegisterIdentityTask task);

		Task<IdentityInfo> GetIdentityInfo(string identityID);

		#endregion Identities

		#region Accounts

		Task<AccountInfo> GetAccountInfo(string accountID);

		Task<decimal> GetBalance(string accountID);

		Task<AccountEntry[]> GetAccountEntries(string accountID);

		Task RemoveStaleAccount(string accountID, string managerID);

		#endregion Accounts

		#region Origination

		Task<CreateContractTask> PrepareCreateContract(CreateContractTask request);

		Task<CreateContractTask> CreateContract(CreateContractTask contract);

		#endregion Origination

		#region Activation

		Task<ActivateIdentityTask> ActivateIdentity(ActivateIdentityTask task);

		#endregion Activation

		#region Transfer

		Task<TransferTask> PrepareTransfer(TransferTask task);

		Task<TransferTask> Transfer(TransferTask task);

		#endregion Transfer

		#region Connect / Disconnect

		Task<ConnectionState> Connect(InstanceInfo registration);

		void Disconnect();

		Task Monitor(IEnumerable<string> accountIDs);

		#endregion Connect / Disconnect

		Task<OperationStatus> GetOperationStatus(OperationTask task);
	}
}