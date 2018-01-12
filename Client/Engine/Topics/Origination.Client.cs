using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Model;
	using Protocol;

	partial class WalletEngine
	{
		public async Task<CreateContractTask> CreateAccount(string name, Identity managerIdentity, TokenStore source, decimal transferAmount)
		{
			Trace("Create Account");

			// Prepare
			var task = new CreateContractTask
			{
				Name = name,
				ManagerID = managerIdentity.AccountID,

				SourceID = source.AccountID,
				SourceManagerPublicKey = managerIdentity.Keys.PublicKey.ToString(),
				NetworkFee = DefaultOperationFee,
				TransferAmount = transferAmount,
			};

			Trace("Prepare CreateContract");
			task = await Connection.PrepareCreateContract(task);

			if (await Sign(task, source.Manager))
			{
				// Create
				Trace("Execute CreateContract");

				return await Connection.CreateContract(task);
			}

			return null;
		}
	}
}