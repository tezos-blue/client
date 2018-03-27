using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Tests
{
	using Model;
	using Protocol;

	[TestClass]
	public class FailureTest : ClientTest
	{
		private Identity identity;

		[TestInitialize]
		public async Task BeforeEach()
		{
			await ConnectToSimulation();

			identity = await Engine.AddIdentity("Stereotype", "Identity", "", true);

			await identity.WhenInitialized;
		}

		[TestMethod]
		public async Task Fail_Originate_Prepare()
		{
			Connection.IsOnline = false;

			var flow = await Engine.CreateAccount("Account", identity, identity, 1);

			await flow.WhenCompleted;

			Assert.AreEqual(TaskProgress.Failed, flow.Task.Progress);
			Assert.IsTrue(flow.IsFailed);
		}

		[TestMethod]
		public async Task Fail_Originate_Submit()
		{
			Connection.FailAfter(1);

			var flow = await Engine.CreateAccount("Account", identity, identity, 1);

			await flow.WhenCompleted;

			Assert.AreEqual(TaskProgress.Failed, flow.Task.Progress);
			Assert.IsTrue(flow.IsFailed);
		}

		[TestMethod]
		public async Task Fail_Originate_Cancel()
		{
			Engine.ApprovalRequired += approval => approval.Approve(false);

			var flow = await Engine.CreateAccount("Account", identity, identity, 1);

			await flow.WhenCompleted;

			Assert.AreEqual(TaskProgress.Cancelled, flow.Task.Progress);
			Assert.IsTrue(flow.IsFailed);
		}

		[TestMethod]
		public async Task Fail_Transfer_Prepare()
		{
			var account = await CreateDestinationAccount();

			Connection.IsOnline = false;

			var flow = await Engine.CommitTransfer(identity, account, 1);

			await flow.WhenCompleted;

			Assert.AreEqual(TaskProgress.Failed, flow.Task.Progress);
			Assert.IsTrue(flow.IsFailed);
		}

		[TestMethod]
		public async Task Fail_Transfer_Submit()
		{
			var account = await CreateDestinationAccount();

			Connection.FailAfter(1);

			var flow = await Engine.CommitTransfer(identity, account, 1);

			await flow.WhenCompleted;

			Assert.AreEqual(TaskProgress.Failed, flow.Task.Progress);
			Assert.IsTrue(flow.IsFailed);
		}

		[TestMethod]
		public async Task Fail_Transfer_Cancel()
		{
			var account = await CreateDestinationAccount();

			Engine.ApprovalRequired += approval => approval.Approve(false);

			var flow = await Engine.CommitTransfer(identity, account, 1);

			await flow.WhenCompleted;

			Assert.AreEqual(TaskProgress.Cancelled, flow.Task.Progress);
			Assert.IsTrue(flow.IsFailed);
		}

		private async Task<Account> CreateDestinationAccount()
		{
			var flow = await Engine.CreateAccount("Account", identity, identity, 1);

			await Connection.CreateBlock();

			await flow.WhenCompleted;
			await WhenMessagesDelivered();

			return identity[flow.Task.AccountID] as Account;
		}
	}
}