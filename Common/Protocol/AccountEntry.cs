using System;
using System.Linq;
using System.Collections.Generic;

namespace SLD.Tezos.Protocol
{
	// Only add to end, never retire
	public enum AccountEntryItemKind
	{
		Invalid,
		Origination,
		Transfer,
		Internal,
		Delegation,
		Activation,
		Freeze,
		Aggregate,
	}

	public class AccountEntry : TezosObject
	{
		public List<AccountEntryItem> Items = new List<AccountEntryItem>();

		public AccountEntry()
		{
		}

		public AccountEntry(int blockIndex, int operationIndex, DateTime time)
		{
			BlockIndex = blockIndex;
			OperationIndex = operationIndex;
			TimeGMT = time;
		}

		public DateTime TimeGMT { get; set; }

		public int BlockIndex { get; set; }
		public int OperationIndex { get; set; }

		public decimal NetworkFee { get; set; }
		public decimal StorageFee { get; set; }
		public decimal ServiceFee { get; set; }

		public decimal Fees => NetworkFee + StorageFee + ServiceFee;

		public decimal Balance { get; set; }

		public string OperationID { get; set; }

		public string Reference { get; set; }

		public override string ToString()
		{
			return $"{BlockIndex}: {Items.Count()} items | {Balance}";
		}
	}

	public class AccountEntryItem : TezosObject
	{
		public AccountEntryItemKind Kind { get; set; }

		public string ContraAccountID { get; set; }

		public decimal Amount { get; set; }

		public override string ToString()
		{
			return $"{Kind} | {ContraAccountID} | {Amount}";
		}
	}
}