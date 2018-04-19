using System;
using System.Collections.Generic;

namespace SLD.Tezos.Protocol
{
	public enum AccountEntryItemKind
	{
		Invalid,
		Origination,
		Transfer,
		Internal,
		Delegation,
		Activation,
	}

	public class AccountEntry : TezosObject
	{
		public List<AccountEntryItem> Items = new List<AccountEntryItem>();

		public AccountEntry()
		{
		}

		public AccountEntry(int index, DateTime time)
		{
			BlockIndex = index;
			TimeGMT = time;
		}

		public DateTime TimeGMT { get; set; }

		public int BlockIndex { get; set; }

		public decimal NetworkFee { get; set; }
		public decimal StorageFee { get; set; }

		public decimal Fees => NetworkFee + StorageFee;
		public decimal Balance { get; set; }

		public string OperationID { get; set; }

		public string Reference { get; set; }

		public override string ToString()
		{
			return $"{BlockIndex}: {Items.Count} items | {Balance}";
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