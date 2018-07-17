using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;

namespace SLD.Tezos.Client.Tests
{
	using Model;
	using Protocol;

	[TestClass]
	public class NotificationTest : ClientTest
	{
		private string currentOperation;

		private TokenStore Source
			=> Engine.DefaultIdentity;

		[TestInitialize]
		public async Task BeforeEach()
		{
			currentOperation = "operation";
			await ConnectToSimulation();

			Assert.IsNull(Engine.DefaultIdentity);

			await Engine.AddIdentity("Stereotype", "Identity", "", true);
		}

		[TestMethod]
		public async Task Notify_Originate_SDDS()
		{
			await SendTransferPending(Source.AccountID);
			await SendOriginatePending("New");

			Assert.AreEqual(2, Engine.DefaultIdentity.Accounts.Count());
			var account = Engine.DefaultIdentity.Accounts[1];

			Assert.AreEqual(TokenStoreState.Creating, account.State);

			// Account
			Assert.AreEqual(1, account.PendingChanges.Count());

			// Source
			Assert.AreEqual(1, Source.PendingChanges.Count());

			await SendOriginate("New");
			await SendBalanceChanged(Source.AccountID);

			// Account
			Assert.AreEqual(TokenStoreState.Online, account.State);
			Assert.AreEqual(0, account.PendingChanges.Count());

			// Source
			Assert.AreEqual(0, Source.PendingChanges.Count());
		}

		[TestMethod]
		public async Task Notify_Originate_DSSD()
		{
			await SendOriginatePending("New");
			await SendTransferPending(Source.AccountID);

			Assert.AreEqual(2, Engine.DefaultIdentity.Accounts.Count());
			var account = Engine.DefaultIdentity.Accounts[1];

			Assert.AreEqual(TokenStoreState.Creating, account.State);

			// Account
			Assert.AreEqual(1, account.PendingChanges.Count());

			// Source
			Assert.AreEqual(1, Source.PendingChanges.Count());

			await SendBalanceChanged(Source.AccountID);
			await SendOriginate("New");

			// Account
			Assert.AreEqual(TokenStoreState.Online, account.State);
			Assert.AreEqual(0, account.PendingChanges.Count());

			// Source
			Assert.AreEqual(0, Source.PendingChanges.Count());
		}

		[TestMethod]
		public async Task Notify_Originate_DSDS()
		{
			await SendOriginatePending("New");
			await SendTransferPending(Source.AccountID);

			Assert.AreEqual(2, Engine.DefaultIdentity.Accounts.Count());
			var account = Engine.DefaultIdentity.Accounts[1];

			Assert.AreEqual(TokenStoreState.Creating, account.State);

			// Account
			Assert.AreEqual(1, account.PendingChanges.Count());

			// Source
			Assert.AreEqual(1, Source.PendingChanges.Count());

			await SendOriginate("New");
			await SendBalanceChanged(Source.AccountID);

			// Account
			Assert.AreEqual(TokenStoreState.Online, account.State);
			Assert.AreEqual(0, account.PendingChanges.Count());

			// Source
			Assert.AreEqual(0, Source.PendingChanges.Count());
		}

		[TestMethod]
		public async Task Notify_Originate_SDSD()
		{
			await SendTransferPending(Source.AccountID);
			await SendOriginatePending("New");

			Assert.AreEqual(2, Engine.DefaultIdentity.Accounts.Count());
			var account = Engine.DefaultIdentity.Accounts[1];

			Assert.AreEqual(TokenStoreState.Creating, account.State);

			// Account
			Assert.AreEqual(1, account.PendingChanges.Count());

			// Source
			Assert.AreEqual(1, Source.PendingChanges.Count());

			await SendBalanceChanged(Source.AccountID);
			await SendOriginate("New");

			// Account
			Assert.AreEqual(TokenStoreState.Online, account.State);
			Assert.AreEqual(0, account.PendingChanges.Count());

			// Source
			Assert.AreEqual(0, Source.PendingChanges.Count());
		}

		private async Task SendTransferPending(string account)
		{
			Connection.FireEventReceived(new TransactionPendingEvent
			{
				OperationID = currentOperation,
				AccountID = account,
			});

			await SmallDelay;
		}

		private async Task SendOriginatePending(string account)
		{
			Connection.FireEventReceived(new OriginatePendingEvent
			{
				OperationID = currentOperation,
				AccountID = account,

				ManagerID = Engine.DefaultIdentity.AccountID,
				Name = account,
			});

			await SmallDelay;
		}

		private async Task SendOriginate(string account)
		{
			Connection.FireEventReceived(new OriginateEvent
			{
				OperationID = currentOperation,
				AccountID = account,

				ManagerID = Engine.DefaultIdentity.AccountID,
				Name = account,
			});

			await SmallDelay;
		}

		private async Task SendBalanceChanged(string account)
		{
			Connection.FireEventReceived(new BalanceChangedEvent
			{
				OperationID = currentOperation,
				AccountID = account,
			});

			await SmallDelay;
		}
	}
}