using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Connections
{
	using Model;
	using Protocol;

	public abstract partial class Connection : TezosObject
	{
		public event Action<NetworkEvent> EventReceived;

		protected void FireEventReceived(NetworkEvent networkEvent)
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

		#region Accounts

		public abstract Task<AccountInfo> GetAccountInfo(string accountID);

		public abstract Task<decimal> GetBalance(string accountID);

		public abstract Task<AccountEntry[]> GetAccountEntries(string accountID);

		#endregion Accounts

		#region Origination

		public abstract Task<CreateFaucetTask> AlphaCreateFaucet(CreateFaucetTask task);

		public abstract Task<CreateContractTask> PrepareCreateContract(CreateContractTask request);

		public abstract Task<CreateContractTask> CreateContract(CreateContractTask contract);

		#endregion Origination

		#region Transfer

		public abstract Task<TransferTask> PrepareTransfer(TransferTask task);

		public abstract Task<TransferTask> Transfer(TransferTask task);

		#endregion Transfer

		#region Connect / Disconnect

		public abstract Task Connect(InstanceInfo registration);

		public abstract void Disconnect();

		public abstract Task Monitor(IEnumerable<string> accountIDs);

		#endregion Connect / Disconnect
	}
}