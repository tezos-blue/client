using System;
using System.Collections.Generic;

namespace SLD.Tezos.Protocol
{


	public class AccountEntry : TezosObject
	{
		public DateTime TimeGMT { get; set; }

		public int BlockIndex { get; set; }

		public decimal NetworkFee { get; set; }
		public decimal StorageFee { get; set; }

		public decimal Fees => NetworkFee + StorageFee;

		public List<AccountEntryItem> Items = new List<AccountEntryItem>();

		public decimal Balance { get; set; }

		public string OperationID { get; set; }

		public string Reference { get; set; }

		public AccountEntry()
		{
		}

		public AccountEntry(int index, DateTime time)
		{
			BlockIndex = index;
			TimeGMT = time;
		}

		public override string ToString()
		{
			return $"{BlockIndex}: {Items.Count} items | {Balance}";
		}
	}

	public enum AccountEntryItemKind
	{
		Invalid,
		Origination,
		Transfer,
		Internal,
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