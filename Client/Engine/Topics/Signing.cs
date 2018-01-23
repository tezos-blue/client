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
		
		IProvideSigning interface

		To be implemented by keepers of private keys.
		Minimum interface to identity services in the Engine.

	*/

	/// <summary>
	/// Signing services for identities
	/// </summary>
	public interface IProvideSigning
	{
		/// <summary>
		/// Enumerate managed identities
		/// </summary>
		/// <value>IDs of managed identities</value>
		IEnumerable<string> IdentityIDs { get; }

		/// <summary>
		/// Initialize on registration
		/// </summary>
		/// <remarks>After this has completed, all other members are supposed to be callable</remarks>
		Task Initialize();

		/// <summary>
		/// Create a signature on data
		/// </summary>
		/// <returns><c>true</c>, if signature is valid; <c>false</c>, if failed or canceled</returns>
		Task<bool> Sign(string identityID, byte[] data, out byte[] signature);

		/// <summary>
		/// Determines whether an identity 
		/// </summary>
		/// <returns><c>true</c> if can sign for the identity</returns>
		bool Contains(string identityID);


		/// <summary>
		/// Gets the public key for an identity.
		/// </summary>
		byte[] GetPublicKey(string identityID);
	}

	/*
		
		Approval and Signing

		Before finally signing an operation, applications can inject one or more approval mechanisms.
		This will give the user an additional step to review what he is about to sign.
		Automated clearance is another use case.

		All Signing in the Engine happens here.

	*/
	partial class Engine
	{
		/// <summary>
		/// Register an approval mechanism.
		/// </summary>
		/// <remarks>The first to complete the approval wins</remarks>
		public event Action<Approval> ApprovalRequired;

		// The signing function _____________________________________________________________________

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

			if (await provider.Sign(signingIdentity.AccountID, operationData, out byte[] signature))
			{
				Guard.ApplySignature(task, operationData, signature);

				return true;
			}

			return false;
		}
	}
}