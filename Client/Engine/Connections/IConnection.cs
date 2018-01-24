using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Connections
{
	using Protocol;

	public interface IConnection
	{
		event Action<NetworkEvent> EventReceived;

		#region Accounts

		Task<IdentityInfo> GetIdentityInfo(string identityID);

		Task<AccountInfo> GetAccountInfo(string accountID);

		Task<decimal> GetBalance(string accountID);

		Task<AccountEntry[]> GetAccountEntries(string accountID);

		Task RemoveStaleAccount(string accountID);

		#endregion Accounts

		#region Origination

		Task<CreateFaucetTask> AlphaCreateFaucet(CreateFaucetTask task);

		Task<CreateContractTask> PrepareCreateContract(CreateContractTask request);

		Task<CreateContractTask> CreateContract(CreateContractTask contract);

		#endregion Origination

		#region Transfer

		Task<TransferTask> PrepareTransfer(TransferTask task);

		Task<TransferTask> Transfer(TransferTask task);

		#endregion Transfer

		#region Connect / Disconnect

		Task Connect(InstanceInfo registration);

		void Disconnect();

		Task Monitor(IEnumerable<string> accountIDs);

		#endregion Connect / Disconnect
	}
}