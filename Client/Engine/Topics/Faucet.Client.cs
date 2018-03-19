using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Flow;
	using Model;
	using Protocol;

	partial class Engine
	{
		public async Task<FaucetFlow> AlphaCreateFaucetAccount(string name, Identity managerIdentity)
		{
			Trace("Create Faucet Account");

			var task = await Connection.AlphaCreateFaucet(new CreateFaucetTask
			{
				ManagerID = managerIdentity.AccountID,
				Name = name,
				Stereotype = Account.DefaultStereotype,
			});

			Trace($"{task.AccountID} | Waiting for creation");

			return new FaucetFlow(task);
		}

		public class FaucetFlow : ProtectedTaskflow
		{
			public FaucetFlow(CreateFaucetTask task) : base(task)
			{
			}
		}
	}
}