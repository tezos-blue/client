using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Protocol;
	using Model;

	partial class WalletEngine
	{
		public async Task<CreateFaucetTask> AlphaCreateFaucetAccount(string name, Identity managerIdentity)
		{
			Trace("Create Faucet Account");

			var result = await Connection.AlphaCreateFaucet(new CreateFaucetTask
			{
				ManagerID = managerIdentity.AccountID,
				Name = name,
			});

			Trace($"{result.AccountID} | Waiting for creation");

			return result;
		}
	}
}