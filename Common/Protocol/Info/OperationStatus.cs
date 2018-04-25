using System;

namespace SLD.Tezos.Protocol
{
	public class OperationStatus
	{
		public string OperationID;

		public OperationEvent NewEvent;

		public TimeSpan RetryAfter;
	}
}