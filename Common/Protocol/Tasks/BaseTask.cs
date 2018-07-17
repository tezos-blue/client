namespace SLD.Tezos.Protocol
{
	public enum TaskProgress
	{
		Created,
		Prepared,
		Submitted,
		Acknowledged,
		Confirmed,
		Timeout,
		Failed,
		Cancelled,
	}

	public class BaseTask
	{
		public ClientInfo Client;
		public TaskProgress Progress;
	}

	public class OperationTask : BaseTask
	{
		public string OperationID { get; set; }
		public string SourceID { get; set; }
	}

	public class TransactionTask : OperationTask
	{
		public string DestinationID { get; set; }
		public decimal TransferAmount { get; set; }

		#region Fees

		public decimal NetworkFee;
		public decimal ServiceFee;
		public decimal StorageFee;

		public decimal Fees => NetworkFee + StorageFee + ServiceFee;

		#endregion Fees

		public virtual decimal TotalAmount => TransferAmount + Fees;
	}

	public class ProtectedTask : TransactionTask
	{
		public string ChainID;
		public string BranchID;

		public string SourceManagerPublicKey;
		public string Operation { get; set; }
		public string SignedOperation { get; set; }
		public string Signature { get; set; }

		public string Contents { get; set; }

		public bool HasSource => !string.IsNullOrEmpty(SourceID);

		public override string ToString()
			=> OperationID;
	}
}