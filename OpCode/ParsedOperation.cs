using System;
using System.Collections.Generic;
using System.Linq;

namespace SLD.Tezos.Client
{
	using Cryptography;

	public class ParsedOperation
	{
		public ParsedOperation(byte[] operationData)
		{
			this.operationData = operationData;

			// Branch
			Branch = CryptoServices.EncodePrefixed(HashType.Block, Take(32));

			// Items
			while (!EndOfData)
			{
				ParseItem();
			}
		}

		private void ParseItem()
		{
			var kind = TakeOne();

			switch (kind)
			{
				case 4:
					ParseActivation();
					break;

				case 7:
					ParseReveal();
					break;

				case 8:
					ParseTransaction();
					break;

				case 9:
					ParseOrigination();
					break;

				case 10:
					ParseDelegation();
					break;

				default:
					throw new NotImplementedException();
			}
		}

		private void ParseReveal()
		{
			var header = ParseHeader();

			var what = TakeOne();

			var publicKey = CryptoServices.EncodePrefixed(HashType.Public, Take(32));
		}

		private void ParseTransaction()
		{
			var header = ParseHeader();

			var amount = ParseAmount();
			var destinationID = ParseAccountID();

			var hasParameters = ParseBool();

			if (hasParameters)
			{
				throw new NotImplementedException("Parameters not implemented yet.");
			}

			Transfers.Add(new Transfer
			{
				Kind = "Transfer",

				SourceID = header.SourceID,
				DestinationID = destinationID,
				Amount = amount,
				Fee = header.Fee,
			});
		}

		private void ParseOrigination()
		{
			var header = ParseHeader();

			var managerID = ParseIdentityID();

			var balance = ParseAmount();

			var spendable = ParseBool();
			var delegatable = ParseBool();

			var hasDelegate = ParseBool();

			if (hasDelegate)
			{
				var delegateID = ParseIdentityID();
			}

			var hasScript = ParseBool();

			if (hasScript)
			{
				throw new NotImplementedException("Scripts not implemented yet.");
			}

			Transfers.Add(new Transfer
			{
				Kind = "Origination",

				SourceID = header.SourceID,
				Amount = balance,
				Fee = header.Fee,
			});
		}

		private void ParseDelegation()
		{
			var header = ParseHeader();

			var isDelegated = ParseBool();

			if (isDelegated)
			{
				var delegateID = ParseIdentityID();
			}
		}

		private void ParseActivation()
		{
			// Not implemented because it's neither security relevant nor is there anything to validate
			Take(40);
		}

		#region Result

		public string Branch { get; private set; }

		public List<Transfer> Transfers { get; private set; } = new List<Transfer>();

		public decimal NetworkFees => Transfers.Sum(t => t.Fee);

		public class Transfer
		{
			public string Kind { get; internal set; }

			public string SourceID { get; internal set; }
			public string DestinationID { get; internal set; }

			public decimal Amount { get; internal set; }

			public decimal Fee { get; internal set; }
		}

		#endregion Result

		#region Elements

		private Header ParseHeader() => new Header
		{
			SourceID = ParseAccountID(),
			Fee = ParseAmount(),
			Counter = ParseNumber(),
			GasLimit = ParseNumber(),
			StorageLimit = ParseNumber(),
		};

		private string ParseAccountID()
		{
			switch (TakeOne())
			{
				case 0: // Identity
					return ParseIdentityID();

				case 1: // Account
					var data = Take(20);
					TakeOne(); // Padding
					return CryptoServices.EncodePrefixed(HashType.Account, data);

				default:
					throw new NotImplementedException();
			}
		}

		private string ParseIdentityID()
		{
			HashType hashType;

			switch (TakeOne())
			{
				case 0:
					hashType = HashType.PublicKeyHash;
					break;

				case 1:
					hashType = HashType.PublicKeyHash2;
					break;

				case 2:
					hashType = HashType.PublicKeyHash3;
					break;

				default:
					throw new NotImplementedException();
			}

			return CryptoServices.EncodePrefixed(hashType, Take(20));
		}

		private ulong ParseNumber()
		{
			ulong number = 0;

			int i;

			for (i = 0; ; i++)
			{
				var next = TakeOne();

				var flag = (next & 128);
				ulong value = (ulong)(next & 127);

				number += value << 7 * i;

				if (flag == 0)
				{
					break;
				}
			}

			return number;
		}

		private decimal ParseAmount() => (decimal)ParseNumber() / 1000000;

		private bool ParseBool() => TakeOne() == 0xFF;

		private class Header
		{
			public string SourceID { get; internal set; }

			public decimal Fee { get; internal set; }

			public ulong Counter { get; internal set; }
			public ulong GasLimit { get; internal set; }
			public ulong StorageLimit { get; internal set; }
		}

		#endregion Elements

		#region Traversing

		private byte[] operationData;
		private int index;

		bool EndOfData => index == operationData.Length;

		private byte[] Peek
		{
			get
			{
				var remaining = operationData.Length - index;

				var buffer = new byte[remaining];

				Buffer.BlockCopy(operationData, index, buffer, 0, remaining);

				return buffer;
			}
		}

		private byte TakeOne() => operationData[index++];

		private byte[] Take(int count)
		{
			var buffer = new byte[count];

			Buffer.BlockCopy(operationData, index, buffer, 0, count);

			index += count;

			return buffer;
		}

		#endregion Traversing
	}
}