using System;

namespace SLD.Tezos.Protocol
{
	public class OperationStatus
	{
		public string OperationID;

		public TaskProgress Progress;

		public OperationEvent[] Events;

		public TimeSpan RetryAfter;
	}
}