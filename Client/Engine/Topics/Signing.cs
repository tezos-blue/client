using System;
using System.Threading;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Cryptography;
	using Model;
	using Protocol;

	public class Approval : ClientObject
	{
		private ManualResetEvent signalComplete = new ManualResetEvent(false);

		public event Action TimedOut;

		public ProtectedTask Task { get; internal set; }
		public bool NeedPassphrase { get; internal set; }
		public string Passphrase { get; set; }
		public string LastError { get; internal set; }
		public bool IsApproved { get; private set; }

		public void SetComplete(bool isApproved)
		{
			IsApproved = isApproved;

			signalComplete.Set();
		}

		internal async Task<bool> Complete()
		{
			return await System.Threading.Tasks.Task.Run(async () =>
			{
				if (signalComplete.WaitOne(Engine.ApprovalTimeout))
				{
					return IsApproved;
				}

				// Timeout
				if (TimedOut != null)
				{
					await ExecuteSynchronized(TimedOut);
				}

				return false;
			});
		}
	}

	partial class Engine
	{
		public event Action<Approval> ApprovalRequired;

		public async Task<bool> Sign(ProtectedTask task, Identity managerIdentity)
		{
			// Approval mechanism registered?
			if (ApprovalRequired != null)
			{
				var approval = new Approval
				{
					Task = task,
					NeedPassphrase = !managerIdentity.IsUnlocked,
				};

				await ExecuteSynchronized(() =>
				{
					ApprovalRequired(approval);
				});

				if (!await approval.Complete())
				{
					return false;
				}

				// Unlock identity if needed
				if (approval.NeedPassphrase)
				{
					managerIdentity.Unlock(approval.Passphrase);
				}
			}

			// Sign
			Trace("Sign CreateRequest");
			task.Signature = CryptoServices.CreateSignature(
					managerIdentity.Keys.PrivateKey.AccessData(),
					task.Operation.HexToByteArray()
					);

			task.SignedOperation = (CryptoServices.AppendSignature(
					managerIdentity.Keys.PrivateKey.AccessData(),
					task.Operation.HexToByteArray()
					))
					.ToHexString();

			return true;
		}
	}
}