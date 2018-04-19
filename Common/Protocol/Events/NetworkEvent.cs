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

	#region Transaction

	public class TransactionTimeoutEvent : OperationEvent
	{
		public string AccountID;

		public override string ToString()
		{
			return $"{AccountID} | Timeout for transfer: {OperationID}";
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

	#endregion Transaction

	#region Origination

	public class OriginationTimeoutEvent : TransactionTimeoutEvent
	{
		public string ManagerID;

		public override string ToString()
		{
			return $"{AccountID} | Timeout for origination: {OperationID}";
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

	public class OriginateEvent : BalanceChangedEvent
	{
		public string ManagerID;
		public string Name;

		public override string ToString()
		{
			return $"Originate {AccountID} with Balance: {Balance}";
		}
	}

	#endregion Origination

	#region Activation

	public class ActivationTimeoutEvent : OperationEvent
	{
		public string IdentityID;

		public override string ToString()
		{
			return $"{IdentityID} | Timeout for activation: {OperationID}";
		}
	}

	public class ActivationPendingEvent : OperationEvent
	{
		public string IdentityID;

		public decimal Amount;

		public override string ToString()
		{
			return $"{IdentityID} | Pending activation: {Amount}";
		}
	}

	#endregion Activation
}