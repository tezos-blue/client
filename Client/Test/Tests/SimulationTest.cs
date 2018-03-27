using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Protocol;
	using Simulation;
	using Tools;

	[TestClass]
	public class SimulationTest : ClientTest
	{
		private const decimal StartBalance = 100m;
		private const decimal TransferAmount = 10m;
		private const decimal StorageFee = 1m;
		private const decimal NetworkFee = 0.1m;
		private const decimal ServiceFee = 0.01m;
		private NetworkSimulation simulation;

		[TestInitialize]
		public void BeforeEach()
		{
			simulation = new NetworkSimulation();
		}

		[TestMethod]
		public async Task Simulation_Connect()
		{
			var endpoint = simulation.RegisterConnection("InstanceID");

			var monitor = new NetworkEventMonitor(endpoint);

			await simulation.Hub.Broadcast(new NetworkEvent());

			Assert.AreEqual(1, monitor.MessageCount);
		}

		[TestMethod]
		public async Task Simulation_RegisterIdentity()
		{
			var monitor = ConnectIdentity("IdentityID");

			Assert.AreEqual(0, monitor.MessageCount);

			await simulation.Hub.Notify("IdentityID", new NetworkEvent());

			Assert.AreEqual(1, monitor.MessageCount);
		}

		[TestMethod]
		public async Task Simulation_Originate()
		{
			var monitor = ConnectIdentity("IdentityID");

			simulation.SetBalance("IdentityID", 100);

			var task = new CreateContractTask
			{
				ManagerID = "IdentityID",
				SourceID = "IdentityID",
				Name = "Identity",
				TransferAmount = TransferAmount,
				NetworkFee = NetworkFee,
				StorageFee = StorageFee,
				ServiceFee = ServiceFee,
			};

			task = simulation.PrepareCreateContract(task);

			await simulation.Hub.WhenPendingSent;
			monitor.AssertCount(0);

			task = simulation.CreateContract(task, "InstanceID");

			await simulation.Hub.WhenPendingSent;
			monitor.AssertCount(2);

			var transferPending = monitor.Single<TransactionPendingEvent>();

			Assert.AreEqual(
				-task.TotalAmount,
				transferPending.Amount
				);

			var originatePending = monitor.Single<OriginatePendingEvent>();

			Assert.AreEqual(
				task.TransferAmount,
				originatePending.Amount
				);

			monitor.Clear();

			simulation.CreateBlock();
			await simulation.Hub.WhenPendingSent;

			monitor.AssertCount(2);

			// Source
			var balanceChanged = monitor.Single<BalanceChangedEvent>();

			var expectedBalance = StartBalance - task.TotalAmount;

			Assert.AreEqual(expectedBalance, balanceChanged.Balance);

			var entry = balanceChanged.Entry;
			Assert.IsNotNull(entry);

			// Destination
			var originate = monitor.Single<OriginateEvent>();

			Assert.AreEqual(
				task.TransferAmount,
				originate.Balance
				);

			entry = originate.Entry;
			Assert.IsNotNull(entry);
		}

		#region Helpers

		private NetworkEventMonitor Connect()
		{
			var endpoint = simulation.RegisterConnection("InstanceID");

			return new NetworkEventMonitor(endpoint);
		}

		private NetworkEventMonitor ConnectIdentity(string identityID)
		{
			var monitor = Connect();

			var task = new RegisterIdentityTask
			{
				IdentityID = identityID,

				Client = new ClientInfo
				{
					InstanceID = "InstanceID"
				}
			};

			simulation.RegisterIdentity(task);

			return monitor;
		}

		#endregion Helpers
	}
}