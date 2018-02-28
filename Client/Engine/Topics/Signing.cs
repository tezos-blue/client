using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Model;
	using Protocol;
	using Security;

	/// <summary>
	/// Result of a signing process
	/// </summary>
	public enum SigningResult
	{
		/// <summary>
		/// Process is underway
		/// </summary>
		Pending,

		/// <summary>
		/// Approved by user, not successfully signed yet
		/// </summary>
		Approved,

		/// <summary>
		/// Successfully signed
		/// </summary>
		Signed,

		/// <summary>
		/// Cancelled by user
		/// </summary>
		Cancelled,

		/// <summary>
		/// Not decided by user withing Approval.Timeout 
		/// </summary>
		Timeout,

		/// <summary>
		/// Could not unlock identity with given credentials
		/// </summary>
		InvalidCredentials,

		/// <summary>
		/// The provider failed to sign the transaction
		/// </summary>
		ProviderFailed,
	}

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
			Approval approval = null;

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
				approval = new Approval
				{
					Task = task,
					Signer = signingIdentity,
					Timeout = ApprovalTimeout,
				};

				// Send approval to UI and wait for completion
				await ExecuteSynchronized(() =>
				{
					ApprovalRequired(approval);
				});

				WaitForUser:
				Trace("Wait for user decision");

				var userResult = await approval.GetUserResult();

				if (userResult != SigningResult.Approved)
				{
					// Cancel or timeout
					approval.Close(userResult);
					return false;
				}

				// Unlock identity if needed
				if (signingIdentity.IsLocked)
				{
					if(!signingIdentity.Unlock(approval.Passphrase))
					{
						// Wrong credentials
						approval.Retry(SigningResult.InvalidCredentials);

						goto WaitForUser;
					}
				} 
			}

			// Sign
			Trace("Sign CreateRequest");

			if (await provider.Sign(signingIdentity.AccountID, operationData, out byte[] signature))
			{
				// Success
				Guard.ApplySignature(task, operationData, signature);

				approval?.Close(SigningResult.Signed);
				return true;
			}
			else
			{
				// Provider failure
				approval?.Close(SigningResult.ProviderFailed);
				return false;
			}
		}
	}
}