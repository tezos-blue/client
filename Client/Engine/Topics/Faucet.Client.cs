using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Flow;
	using Model;
	using Protocol;

	partial class Engine
	{
		public async Task<ProtectedTaskflow<CreateFaucetTask>> AlphaCreateFaucetAccount(string name, Identity managerIdentity)
		{
			Trace("Create Faucet Account");

			var task = new CreateFaucetTask
			{
				ManagerID = managerIdentity.AccountID,
				Name = name,
				Stereotype = Account.DefaultStereotype,
			};

			var flow = new ProtectedTaskflow<CreateFaucetTask>(task);

			try
			{
				flow.Task = await Connection.AlphaCreateFaucet(task);
				flow.SetPending();
				Trace($"{task.AccountID} | Waiting for creation");
			}
			catch
			{
				flow.Update(TaskProgress.Failed);
			}

			return flow;
		}
	}
}