using System;

namespace SLD.Tezos.Client
{
	using Protocol;

	partial class Engine
	{
		private void ValidateOperation(ProtectedTask task, byte[] operationData)
		{
			if (configuration.InTestMode)
			{
				return;
			}

			void Fail(string reason)
			{
				var exception = new ApplicationException($"Validation | {reason}");
				Trace(exception);
				throw exception;
			}

			var parsed = new ParsedOperation(operationData);

			// Only main and service item
			if (parsed.Transfers.Count != 2)
			{
				Fail("Too many transfers in operation");
			}

			// Service fee as offered to user
			var serviceTransfer = parsed.Transfers[1];
			if (serviceTransfer.Amount != task.ServiceFee)
			{
				Fail("Wrong service fee");
			}

			// Network fee as offered to user
			var mainTransfer = parsed.Transfers[0];
			if (mainTransfer.Fee != task.NetworkFee)
			{
				Fail("Wrong network fee");
			}

			// Transfer amount
			if (mainTransfer.Amount != task.TransferAmount)
			{
				Fail("Wrong amount");
			}

			// Task specific validation
			switch (task)
			{
				case TransferTask transfer:
					{
						if (mainTransfer.Kind != "Transfer")
						{
							Fail("Wrong task kind");
						}

						if (mainTransfer.DestinationID != transfer.DestinationID)
						{
							Fail("Wrong transfer destination");
						}
					}
					break;

				case CreateContractTask originate:
					{
						if (mainTransfer.Kind != "Origination")
						{
							Fail("Wrong task kind");
						}
					}
					break;

				default:
					break;
			}
		}
	}
}