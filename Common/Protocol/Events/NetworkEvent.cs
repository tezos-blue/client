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
			return $"{AccountID} | Pending: {Amount} against {ContraAccountID}";
		}
	}

	public class OriginatePendingEvent : TransactionPendingEvent
	{
		public string ManagerID;
		public string Name;

		public override string ToString()
		{
			return $"{AccountID} | Pending Originate as '{Name}' with Balance: {Amount}";
		}
	}

	public class TransactionTimeoutEvent : OperationEvent
	{
		public string AccountID;

		public override string ToString()
		{
			return $"{AccountID} | Timeout for transfer: {OperationID}";
		}
	}

	public class OriginationTimeoutEvent : TransactionTimeoutEvent
	{
		public string ManagerID;

		public override string ToString()
		{
			return $"{AccountID} | Timeout for origination: {OperationID}";
		}
	}
}