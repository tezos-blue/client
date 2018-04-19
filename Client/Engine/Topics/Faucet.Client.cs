using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Flow;
	using Model;
	using Protocol;
	using System;

	partial class Engine
	{
		public async Task<OperationTaskflow> ActivateFaucetIdentity(Identity identity, string secret, decimal expectedAmount)
		{
			Trace("Activate Faucet Identity");

			var task = new ActivateIdentityTask
			{
				IdentityID = identity.AccountID,
				Amount = expectedAmount,
				Secret = secret,
			};

			var flow = new OperationTaskflow(task);

			try
			{
				flow.Task = await Connection.ActivateIdentity(task);
				flow.SetPending();
				Trace($"{identity.AccountID} | Waiting for activation");
			}
			catch
			{
				flow.Update(TaskProgress.Failed);
			}

			return flow;
		}

		public Task<ProtectedTaskflow<CreateFaucetTask>> AlphaCreateFaucetAccount(string name, Identity managerIdentity)
		{
			throw new NotSupportedException("No more faucets in the alphanet");
		}
	}
}