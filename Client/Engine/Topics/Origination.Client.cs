using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Flow;
	using Model;
	using Protocol;

	partial class Engine
	{
		public async Task<ProtectedTaskflow<CreateContractTask>> CreateAccount(string name, Identity managerIdentity, TokenStore source, decimal transferAmount, string stereotype = null)
		{
			Trace($"Create Account '{name}' for {managerIdentity}, transferring {transferAmount} from {source}");

			// Create Task and Flow
			var task = new CreateContractTask
			{
				Name = name,
				Stereotype = stereotype ?? Account.DefaultStereotype,

				ManagerID = managerIdentity.AccountID,

				SourceID = source.AccountID,
				SourceManagerPublicKey = managerIdentity.PublicKey.ToString(),
				NetworkFee = DefaultOperationFee,
				TransferAmount = transferAmount,
			};


			var flow = new ProtectedTaskflow<CreateContractTask>(task);

			// Prepare Task
			try
			{
				Trace("Prepare CreateContract");
				flow.Task = task = await Connection.PrepareCreateContract(task);
			}
			catch
			{
				flow.Update(TaskProgress.Failed);
			}

			if (flow.IsFailed)
			{
				return flow;
			}

			// External Signing
			if (await Sign(task, source.Manager))
			{
				try
				{
					// Submit
					Trace("Execute CreateContract");
					flow.Task = await Connection.CreateContract(task);
					flow.SetPending();
				}
				catch
				{
					flow.Update(TaskProgress.Failed);
				}
			}
			else
			{
				flow.Update(TaskProgress.Cancelled);
			}

			return flow;
		}
	}
}