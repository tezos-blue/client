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
	}

	public class ProtectedTask : OperationTask
	{
		public string ChainID;
		public string SourceID;
		public string SourceManagerPublicKey;
		public decimal TransferAmount;
		public string Operation { get; set; }
		public string SignedOperation { get; set; }
		public string Signature { get; set; }

		#region Fees

		public decimal NetworkFee;
		public decimal ServiceFee;
		public decimal StorageFee;

		public decimal Fees => NetworkFee + StorageFee + ServiceFee;

		#endregion Fees

		public bool HasSource => !string.IsNullOrEmpty(SourceID);

		public virtual decimal TotalAmount => TransferAmount + Fees;

		public override string ToString()
			=> OperationID;
	}
}