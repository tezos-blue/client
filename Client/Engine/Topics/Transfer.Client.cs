using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Flow;
	using Model;
	using Protocol;

	partial class Engine
	{
		public async Task<ProtectedTaskflow<TransferTask>> CommitTransfer(TokenStore source, TokenStore destination, decimal transferAmount, string reference = null)
		{
			Trace($"Transfer {transferAmount} from {source} to {destination}");

			// Prepare
			var task = new TransferTask
			{
				SourceID = source.AccountID,
				DestinationID = destination.AccountID,
				SourceManagerPublicKey = source.Manager.PublicKey.ToString(),
				NetworkFee = DefaultOperationFee,
				TransferAmount = transferAmount,

				Reference = reference,
			};

			var flow = new ProtectedTaskflow<TransferTask>(task);

			try
			{
				Trace("Prepare Transfer");
				flow.Task = task = await Connection.PrepareTransfer(task);
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
					flow.Task = await Connection.Transfer(task);
					flow.SetPending(this);
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