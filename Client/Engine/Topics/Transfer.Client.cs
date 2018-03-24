using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Flow;
	using Model;
	using Protocol;

	partial class Engine
	{
		public async Task<ProtectedTaskflow> CommitTransfer(TokenStore source, TokenStore destination, decimal transferAmount, string reference = null)
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

			Trace("Prepare Transfer");
			task = await Connection.PrepareTransfer(task);

			// Sign
			if (await Sign(task, source.Manager))
			{
				// Create
				Trace("Execute Transfer");
				var result = await Connection.Transfer(task);

				Trace($"Transfer committed: {transferAmount} from {source} to {destination}");

				return new ProtectedTaskflow(result);
			}

			return null;
		}
	}
}