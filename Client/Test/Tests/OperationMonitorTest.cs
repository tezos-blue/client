using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Connections;
	using Flow;
	using Protocol;

	[TestClass]
	public class OperationMonitorTest
	{
		[TestMethod]
		public async Task Monitor_NoTimeout()
		{
			var connection = new TestConnection();
			var task = new OperationTask();
			var flow = new OperationTaskflow(task);

			List<OperationEvent> received = new List<OperationEvent>();

			void callback(OperationEvent operationEvent)
			{
				received.Add(operationEvent);
			}

			var monitor = new OperationMonitor(
				flow,
				TimeSpan.FromMilliseconds(200),
				TimeSpan.FromMilliseconds(200),
				connection,
				callback);

			// Time to start monitoring
			await Task.Delay(100);

			monitor.Update(TaskProgress.Acknowledged);

			await Task.Delay(100);

			flow.Update(TaskProgress.Confirmed);

			await Task.Delay(300);

			Assert.AreEqual(0, received.Count);
			Assert.IsTrue(monitor.IsComplete);
		}

		[TestMethod]
		public void Monitor_AcknowledgeLost()
		{
			var connection = new TestConnection();
			var task = new OperationTask();
			var flow = new OperationTaskflow(task);

			AutoResetEvent signal = new AutoResetEvent(false);

			void callback(OperationEvent operationEvent)
			{
				signal.Set();
			}

			connection.NextStatus = new OperationStatus
			{
				Events = new[] { new OperationEvent() },
				RetryAfter = TimeSpan.FromMilliseconds(100)
			};

			var monitor = new OperationMonitor(
				flow,
				TimeSpan.FromMilliseconds(100),
				TimeSpan.FromMilliseconds(2000),
				connection,
				callback);

			Assert.IsTrue(signal.WaitOne(200));
			Assert.IsTrue(signal.WaitOne(200));
			Assert.IsTrue(signal.WaitOne(200));

			monitor.Update(TaskProgress.Confirmed);

			Assert.IsFalse(signal.WaitOne(200));

			Assert.IsTrue(monitor.IsComplete);
		}

		private class TestConnection : IConnection
		{
			public int NumCalled;

			public OperationStatus NextStatus;

			public Task<OperationStatus> GetOperationStatus(OperationTask task)
			{
				NumCalled++;

				return Task.FromResult(NextStatus);
			}

			#region Unused

#pragma warning disable 67

			public event Action<NetworkEvent> EventReceived;

			public Task<RegisterIdentityTask> RegisterIdentity(RegisterIdentityTask task)
			{
				throw new NotImplementedException();
			}

			public Task<AccountInfo> GetAccountInfo(string accountID)
			{
				throw new NotImplementedException();
			}

			public Task<decimal> GetBalance(string accountID)
			{
				throw new NotImplementedException();
			}

			public Task<AccountEntry[]> GetAccountEntries(string accountID)
			{
				throw new NotImplementedException();
			}

			public Task RemoveStaleAccount(string accountID, string managerID)
			{
				throw new NotImplementedException();
			}

			public Task<CreateContractTask> PrepareCreateContract(CreateContractTask request)
			{
				throw new NotImplementedException();
			}

			public Task<CreateContractTask> CreateContract(CreateContractTask contract)
			{
				throw new NotImplementedException();
			}

			public Task<ActivateIdentityTask> ActivateIdentity(ActivateIdentityTask task)
			{
				throw new NotImplementedException();
			}

			public Task<TransferTask> PrepareTransfer(TransferTask task)
			{
				throw new NotImplementedException();
			}

			public Task<TransferTask> Transfer(TransferTask task)
			{
				throw new NotImplementedException();
			}

			public Task<ConnectionState> Connect(InstanceInfo registration)
			{
				throw new NotImplementedException();
			}

			public void Disconnect()
			{
				throw new NotImplementedException();
			}

			public Task Monitor(IEnumerable<string> accountIDs)
			{
				throw new NotImplementedException();
			}

			#endregion Unused
		}
	}
}