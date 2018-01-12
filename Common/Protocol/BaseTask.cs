namespace SLD.Tezos.Protocol
{
	public enum TaskProgress
	{
		Created,
		Prepared,
		Submitted,
		Confirmed,
		Failed,
	}

	public class BaseTask
	{
		public ClientInfo Client;
		public TaskProgress Progress;
	}

	public class ProtectedTask : BaseTask
	{
		public string SourceID;
		public string SourceManagerPublicKey;
		public decimal TransferAmount;
		public string Operation { get; set; }
		public string OperationID { get; set; }
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
	}
}