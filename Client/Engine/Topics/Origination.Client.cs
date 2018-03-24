using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Flow;
	using Model;
	using Protocol;

	partial class Engine
	{
		public async Task<ProtectedTaskflow> CreateAccount(string name, Identity managerIdentity, TokenStore source, decimal transferAmount, string stereotype = null)
		{
			Trace("Create Account");

			// Prepare
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

			Trace("Prepare CreateContract");
			task = await Connection.PrepareCreateContract(task);

			if (await Sign(task, source.Manager))
			{
				// Create
				Trace("Execute CreateContract");

				return new ProtectedTaskflow(await Connection.CreateContract(task));
			}

			return null;
		}
	}
}