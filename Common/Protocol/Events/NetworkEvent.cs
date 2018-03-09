namespace SLD.Tezos.Protocol
{
	public class NetworkEvent : ProtocolObject
	{
	}

	public class OperationEvent : NetworkEvent
	{
		public string OperationID;
	}

	public class BalanceChangedEvent : OperationEvent
	{
		public int BlockIndex;

		public string AccountID;
		public decimal Balance;
		public AccountState State;

		public AccountEntry Entry;

		public override string ToString()
		{
			return $"{AccountID} | Balance: {Balance}";
		}
	}

	public class OriginateEvent : BalanceChangedEvent
	{
		public string ManagerID;
		public string Name;

		public override string ToString()
		{
			return $"Originate {AccountID} with Balance: {Balance}";
		}
	}

	public class TransactionPendingEvent : OperationEvent
	{
		public string AccountID;
		public string ContraAccountID;

		public decimal Amount;

		public override string ToString()
		{
			return $"{AccountID} | Pending: {Amount}";
		}
	}

	public class OriginatePendingEvent : TransactionPendingEvent
	{
		public string ManagerID;
		public string Name;

		public override string ToString()
		{
			return $"{AccountID} | Pending: {Amount}";
		}
	}

	public class TransactionTimeoutEvent : OperationEvent
	{
		public string AccountID;
	}

	public class OriginationTimeoutEvent : TransactionTimeoutEvent
	{
		public string ManagerID;
	}
}