using System;

namespace SLD.Tezos.Protocol
{
	public class OperationStatus
	{
		public string OperationID;

		public OperationEvent SourceEvent;
		public OperationEvent DestinationEvent;

		public TimeSpan RetryAfter;
	}
}