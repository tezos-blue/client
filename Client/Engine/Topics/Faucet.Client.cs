using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Protocol;
	using Model;

	partial class Engine
	{
		public async Task<CreateFaucetTask> AlphaCreateFaucetAccount(string name, Identity managerIdentity)
		{
			Trace("Create Faucet Account");

			var result = await Connection.AlphaCreateFaucet(new CreateFaucetTask
			{
				ManagerID = managerIdentity.AccountID,
				Name = name,
				Stereotype = Account.DefaultStereotype,
			});

			Trace($"{result.AccountID} | Waiting for creation");

			return result;
		}
	}
}