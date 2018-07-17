using System;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Flow;
	using Model;
	using Protocol;

	partial class Engine
	{
		public async Task<ProtectedTaskflow<CreateContractTask>> CreateDelegatedAccount(string name, Identity managerIdentity, TokenStore source, string delegateID, decimal transferAmount, string stereotype = null)
		{
			Trace($"Create Account '{name}' for {managerIdentity}, transferring {transferAmount} from {source}");

			// Create Task and Flow
			var task = new CreateContractTask
			{
				Name = name,
				Stereotype = stereotype ?? Account.DelegatedStereotype,

				ManagerID = managerIdentity.AccountID,
				DelegateID = delegateID,

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
					flow.SetPending(this);
				}
				catch (Exception e)
				{
					Trace(e);
					flow.Update(TaskProgress.Failed);
				}
			}
			else
			{
				Trace("Sign cancelled");
				flow.Update(TaskProgress.Cancelled);
			}

			return flow;
		}
	}
}