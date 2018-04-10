using System;
using System.Threading.Tasks;
using ThreadTask = System.Threading.Tasks.Task;

namespace SLD.Tezos.Client.Security
{
	using Cryptography;
	using Model;
	using Protocol;

	public class Approval : ClientObject
	{
		public Identity Signer { get; internal set; }

		public ProtectedTask Task { get; internal set; }

		public SigningResult Result { get; private set; }

		#region User interface

		private SyncEvent taskUser = new SyncEvent();

		public event Action TimedOut;

		public string PassphraseText
		{
			set
			{
				if (value != null)
				{
					Passphrase = new Passphrase(value);
				}
			}
		}

		public Task<SigningResult> Approve(bool isApproved)
		{
			IsApproved = isApproved;

			taskUser.SetComplete(isApproved);

			return taskSign.WhenComplete;
		}

		#endregion User interface

		#region Engine interface

		private SyncEvent<SigningResult> taskSign = new SyncEvent<SigningResult>();

		public bool IsApproved { get; private set; }

		public Passphrase Passphrase { internal get; set; }

		internal void Close(SigningResult result)
		{
			Result = result;
			taskSign.SetComplete(result);
		}

		internal void Retry(SigningResult result)
		{
			Result = SigningResult.Pending;
			Passphrase = null;
			IsApproved = false;
			taskUser = new SyncEvent();

			var attempt = taskSign;
			taskSign = new SyncEvent<SigningResult>();

			attempt.SetComplete(result);
		}

		internal async Task<SigningResult> GetUserResult()
		{
			var taskTimeout = ThreadTask.Delay(Timeout);

			var winner = await ThreadTask.WhenAny(
				taskTimeout,
				taskUser.WhenComplete
				);

			if (winner == taskTimeout)
			{
				// Timeout
				if (TimedOut != null)
				{
					await ExecuteSynchronized(TimedOut);
				}

				return SigningResult.Timeout;
			}
			else
			{
				// Decided
				return IsApproved ? SigningResult.Approved : SigningResult.Cancelled;
			}
		}

		#endregion Engine interface

		public TimeSpan Timeout { get; internal set; }

		public string LastError { get; internal set; }
	}
}