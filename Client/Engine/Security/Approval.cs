using System;
using System.Threading;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Security
{
	using Cryptography;
	using Protocol;

	public class Approval : ClientObject
	{
		private ManualResetEvent signalComplete = new ManualResetEvent(false);

		public event Action TimedOut;

		public ProtectedTask Task { get; internal set; }
		public bool NeedPassphrase { get; internal set; }
		public string LastError { get; internal set; }
		public bool IsApproved { get; private set; }

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

		internal Passphrase Passphrase { get; private set; }

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
}