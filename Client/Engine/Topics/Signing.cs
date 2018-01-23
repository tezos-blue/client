using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Model;
	using Protocol;
	using Security;

	/*
	 *
	*/

	public interface IProvideSigning
	{
		IEnumerable<string> IdentityIDs { get; }

		Task Initialize();

		Task<bool> Sign(byte[] data, string identityID, out byte[] signature);

		bool Contains(string identityID);

		byte[] GetPublicKey(string identityID);
	}

	partial class Engine
	{
		public event Action<Approval> ApprovalRequired;

		internal async Task<bool> Sign(ProtectedTask task, Identity signingIdentity)
		{
			// Find signing provider
			var provider = signProviders
				.FirstOrDefault(p => p.Contains(signingIdentity.AccountID));

			if (provider == null) return false;

			// Fetch operation data to sign later.
			// This prevents tampering with the operation during approval
			var operationData = task.Operation.HexToByteArray();

			// Approval mechanism registered?
			if (ApprovalRequired != null)
			{
				var approval = new Approval
				{
					Task = task,
					NeedPassphrase = !signingIdentity.IsUnlocked,
				};

				// Send approval to UI and wait for completion
				await ExecuteSynchronized(() =>
				{
					ApprovalRequired(approval);
				});

				if (!await approval.Complete())
				{
					// Cancel or timeout
					return false;
				}

				// Unlock identity if needed
				if (approval.NeedPassphrase)
				{
					signingIdentity.Unlock(approval.Passphrase);
				}
			}

			// Sign
			Trace("Sign CreateRequest");

			if (await provider.Sign(operationData, signingIdentity.AccountID, out byte[] signature))
			{
				Guard.ApplySignature(task, operationData, signature);

				return true;
			}

			return false;
		}
	}
}