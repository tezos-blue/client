using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SLD.Tezos.Client.Tests
{
	[TestClass]
	public class OpCodeTest
	{
		[TestMethod]
		public void OpCode_Transfer()
		{
			string operation = "a9661e1ec8fa9e290544d0475cb52427c2b2475cab952f8c69aa82e4c9245a6d0800005cab14e8519e8b010bcb00edf18cdb911e09ca1eb0ea01d2db02845200c0843d01cbafa3c6a1f002cb193b61350b81488573cbf99600000800005cab14e8519e8b010bcb00edf18cdb911e09ca1e00d3db02845200a09c01000016a8b02f2da606dbfffd815dcb9a956195c5a12100";

			string branch = "BLztQLCrLSFF7eSMFo9WE7KZtVanUSdyZVBFtriRwh3vB6hNZRz";
			string source = "tz1U61ojKMCtNLBqCMs67VRZVXvcxgs3k2MK";
			string destination = "KT1T9m9NTPfQ1FWF7SbvMWxpuK2BQRVViHqS";

			decimal amount = 1M;
			decimal networkFee = 0.03M;
			decimal serviceFee = 0.02M;

			var parsed = new ParsedOperation(operation.HexToByteArray());

			Assert.AreEqual(branch, parsed.Branch);

			Assert.AreEqual(2, parsed.Transfers.Count());

			// Main Transfer
			var transfer = parsed.Transfers[0];

			Assert.AreEqual(source, transfer.SourceID);
			Assert.AreEqual(destination, transfer.DestinationID);
			Assert.AreEqual(amount, transfer.Amount);

			// Service Transfer
			var service = parsed.Transfers[1];

			Assert.AreEqual(source, service.SourceID);
			Assert.AreEqual(serviceFee, service.Amount);

			// Fees
			Assert.AreEqual(networkFee, parsed.NetworkFees);
		}

		[TestMethod]
		public void OpCode_Transfer2()
		{
			string operation = "d1db05e2b6d56bd4aa8476dffe409e28b96535bf35d98b2aa2301818d8662998080000469ecf25f9a4af9b9c8eec1d6446b04bbcf450bfa09c01e1df1a845200c0843d014fff039eab492c3eb7e09329f6f83a1b61fb034f0000080000469ecf25f9a4af9b9c8eec1d6446b04bbcf450bf00e2df1a845200a09c0100009d257d22b1b6714f3fd5e1de5918b1934257c0c900";

			string source = "tz1S5SEoUautkRxpMXbLkzvc3WwsjBXdocpJ";
			string destination = "KT1FskScWdxxjVqgtzVBa6Wg8rppJAGHXSHa";

			decimal amount = 1M;
			decimal networkFee = 0.02M;
			decimal serviceFee = 0.02M;

			var parsed = new ParsedOperation(operation.HexToByteArray());

			Assert.AreEqual(2, parsed.Transfers.Count());

			// Main Transfer
			var transfer = parsed.Transfers[0];

			Assert.AreEqual(source, transfer.SourceID);
			Assert.AreEqual(destination, transfer.DestinationID);
			Assert.AreEqual(amount, transfer.Amount);

			// Service Transfer
			var service = parsed.Transfers[1];

			Assert.AreEqual(source, service.SourceID);
			Assert.AreEqual(serviceFee, service.Amount);

			// Fees
			Assert.AreEqual(networkFee, parsed.NetworkFees);
		}

		[TestMethod]
		public void OpCode_TransferToNewIdentity()
		{
			string operation = "dbd71517c88d91894a9b68397becac162f8426056273862b98ce37dd4fbb8f2e08014fff039eab492c3eb7e09329f6f83a1b61fb034f0088f410018452ac02c0843d00004269f1ce40173460f17d83139cf9fe9d1ad0ea9a0008014fff039eab492c3eb7e09329f6f83a1b61fb034f000002845200a09c0100009d257d22b1b6714f3fd5e1de5918b1934257c0c900";

			string source = "KT1FskScWdxxjVqgtzVBa6Wg8rppJAGHXSHa";
			string destination = "tz1RhCCwH3KMCsibRkGoEecCGRLVr2PuC48B";

			decimal amount = 1M;
			decimal networkFee = 0.277M;
			decimal serviceFee = 0.02M;

			var parsed = new ParsedOperation(operation.HexToByteArray());

			Assert.AreEqual(2, parsed.Transfers.Count());

			// Main Transfer
			var transfer = parsed.Transfers[0];

			Assert.AreEqual(source, transfer.SourceID);
			Assert.AreEqual(destination, transfer.DestinationID);
			Assert.AreEqual(amount, transfer.Amount);

			// Service Transfer
			var service = parsed.Transfers[1];

			Assert.AreEqual(source, service.SourceID);
			Assert.AreEqual(serviceFee, service.Amount);

			// Fees
			Assert.AreEqual(networkFee, parsed.NetworkFees);
		}

		[TestMethod]
		public void OpCode_RevealAndOriginate()
		{
			string operation = "53af5dd733f467d4403b7e03fd3faa0601415cb36545dce33f30724158825b3f070000469ecf25f9a4af9b9c8eec1d6446b04bbcf450bf00d6df1a84520000be5ed8b7c0b04c51431a3cea70e1aa4fb355d6e1102f00b6b34be530e092f877090000469ecf25f9a4af9b9c8eec1d6446b04bbcf450bfc0843dd7df1a8452ac0200469ecf25f9a4af9b9c8eec1d6446b04bbcf450bfc0843dffff0000080000469ecf25f9a4af9b9c8eec1d6446b04bbcf450bf00d8df1a845200a09c0100009d257d22b1b6714f3fd5e1de5918b1934257c0c900";

			string publicKey = "edpkv64hJThwwrhQHvkRFcJB3AnGKFnvc8fDDPYZNEtbqBydTqvjVA";

			string branch = "BLM8xUWjEv761c5xTVTQdNZoYoczi3pMhoXjV3cSoBqnx7jPDWz";
			string source = "tz1S5SEoUautkRxpMXbLkzvc3WwsjBXdocpJ";

			decimal amount = 1M;
			decimal networkFee = 1M;
			decimal serviceFee = 0.02M;

			var parsed = new ParsedOperation(operation.HexToByteArray());

			Assert.AreEqual(branch, parsed.Branch);

			Assert.AreEqual(2, parsed.Transfers.Count());

			// Main Transfer
			var transfer = parsed.Transfers[0];

			Assert.AreEqual(source, transfer.SourceID);
			Assert.AreEqual(amount, transfer.Amount);

			// Service Transfer
			var service = parsed.Transfers[1];

			Assert.AreEqual(source, service.SourceID);
			Assert.AreEqual(serviceFee, service.Amount);

			// Fees
			Assert.AreEqual(networkFee, parsed.NetworkFees);
		}

		[TestMethod]
		public void OpCode_Originate()
		{
			string operation = "4ba55e75d52a668eb08a0f11b6a52e49e2c82980c8bd83a349329bc2066ab12a090000469ecf25f9a4af9b9c8eec1d6446b04bbcf450bfc0843de3df1a8452ac0200469ecf25f9a4af9b9c8eec1d6446b04bbcf450bfc0843dffff0000080000469ecf25f9a4af9b9c8eec1d6446b04bbcf450bf00e4df1a845200a09c0100009d257d22b1b6714f3fd5e1de5918b1934257c0c900";

			string source = "tz1S5SEoUautkRxpMXbLkzvc3WwsjBXdocpJ";

			decimal amount = 1M;
			decimal networkFee = 1M;
			decimal serviceFee = 0.02M;

			var parsed = new ParsedOperation(operation.HexToByteArray());

			Assert.AreEqual(2, parsed.Transfers.Count());

			// Main Transfer
			var transfer = parsed.Transfers[0];

			Assert.AreEqual(source, transfer.SourceID);
			Assert.AreEqual(amount, transfer.Amount);

			// Service Transfer
			var service = parsed.Transfers[1];

			Assert.AreEqual(source, service.SourceID);
			Assert.AreEqual(serviceFee, service.Amount);

			// Fees
			Assert.AreEqual(networkFee, parsed.NetworkFees);
		}

		[TestMethod]
		public void OpCode_RevealAndTransfer()
		{
			string operation = "564843323c10c8235c6e52032c2d70cbcf9c80d47f0e6ed574391c90598419370700001f520596836b5381af80b2ef64b60c43ce721a7700afde1b8452000007f87204ca95c58816e3e6260a12a9599dbe9513b3be1e4e903ad17cb0fde56d0800001f520596836b5381af80b2ef64b60c43ce721a77a09c01b0de1b845200a9bae5e12f0000469ecf25f9a4af9b9c8eec1d6446b04bbcf450bf000800001f520596836b5381af80b2ef64b60c43ce721a7700b1de1b845200a09c0100009d257d22b1b6714f3fd5e1de5918b1934257c0c900";

			string branch = "BLNHJPENSBXk3G2BQSRJPqm2jz9LB2fWPABsFrT23NPuPQtMCYv";
			string source = "tz1NVdu3R3qNz5wmoGLDRgRDs8tDVveMS18d";
			string destination = "tz1S5SEoUautkRxpMXbLkzvc3WwsjBXdocpJ";

			decimal amount = 12821.552425M;
			decimal networkFee = 0.02M;
			decimal serviceFee = 0.02M;

			var parsed = new ParsedOperation(operation.HexToByteArray());

			Assert.AreEqual(branch, parsed.Branch);

			Assert.AreEqual(2, parsed.Transfers.Count());

			// Main Transfer
			var transfer = parsed.Transfers[0];

			Assert.AreEqual(source, transfer.SourceID);
			Assert.AreEqual(destination, transfer.DestinationID);
			Assert.AreEqual(amount, transfer.Amount);

			// Service Transfer
			var service = parsed.Transfers[1];

			Assert.AreEqual(source, service.SourceID);
			Assert.AreEqual(serviceFee, service.Amount);

			// Fees
			Assert.AreEqual(networkFee, parsed.NetworkFees);
		}


		[TestMethod]
		public void OpCode_OriginateAndDelegate()
		{
			string operation = "ec0d95c0da746bd54baa21e1e57cc146680bbee50f93acea7a9e3ca43a0b6309090000469ecf25f9a4af9b9c8eec1d6446b04bbcf450bfc0843dd9df1a8452ac0200469ecf25f9a4af9b9c8eec1d6446b04bbcf450bf80ade204ffffff00b15b7a2484464ed3228c0ae23d0391f8269de3da00080000469ecf25f9a4af9b9c8eec1d6446b04bbcf450bf00dadf1a845200a09c0100009d257d22b1b6714f3fd5e1de5918b1934257c0c900";

			string branch = "BMWEzdKDJU9TPTTMmV1Z4j2cMSqgJpPYBNey1oviagztn7iVVFX";
			string source = "tz1S5SEoUautkRxpMXbLkzvc3WwsjBXdocpJ";
			string delegateID = "tz1boot1pK9h2BVGXdyvfQSv8kd1LQM6H889";

			decimal amount = 10M;
			decimal networkFee = 1M;
			decimal serviceFee = 0.02M;

			var parsed = new ParsedOperation(operation.HexToByteArray());

			Assert.AreEqual(branch, parsed.Branch);

			Assert.AreEqual(2, parsed.Transfers.Count());

			// Main Transfer
			var transfer = parsed.Transfers[0];

			Assert.AreEqual(source, transfer.SourceID);
			Assert.AreEqual(amount, transfer.Amount);

			// Service Transfer
			var service = parsed.Transfers[1];

			Assert.AreEqual(source, service.SourceID);
			Assert.AreEqual(serviceFee, service.Amount);

			// Fees
			Assert.AreEqual(networkFee, parsed.NetworkFees);
		}


	}
}
