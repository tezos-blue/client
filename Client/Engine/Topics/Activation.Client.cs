using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Flow;
	using Model;
	using Protocol;

	partial class Engine
	{
		public async Task<OperationTaskflow> ActivateIdentity(Identity identity, string secret, decimal expectedAmount)
		{
			Trace("Activate Identity");

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
				flow.SetPending(this);

				Trace($"{identity.AccountID} | Waiting for activation");
			}
			catch
			{
				flow.Update(TaskProgress.Failed);
			}

			return flow;
		}
	}
}