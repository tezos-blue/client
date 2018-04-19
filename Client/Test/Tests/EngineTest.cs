using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Flow;
	using Model;
	using Protocol;

	[TestClass]
	public class EngineTest : ClientTest
	{
		[TestInitialize]
		public async Task BeforeEach()
		{
			await ConnectToSimulation();

			Assert.IsNull(Engine.DefaultIdentity);

			await Engine.AddIdentity("Stereotype", "Identity", "", true);
		}

		[TestMethod]
		public async Task Engine_Initialized()
		{
			var identity = Engine.DefaultIdentity;

			Assert.IsNotNull(identity);

			Assert.AreEqual(TokenStoreState.Initializing, identity.State);

			Assert.AreEqual(1, identity.Accounts.Count);

			var identityAccount = identity.Accounts[0];

			Assert.IsNotNull(identityAccount);

			Assert.AreSame(identity, identityAccount);

			var initResult = await identity.WhenInitialized;
			Assert.IsTrue(initResult);

			Assert.AreEqual(TokenStoreState.Online, identityAccount.State);

			Assert.AreEqual(0, identityAccount.PendingChanges.Count);
			Assert.IsFalse(identityAccount.HasPendingChanges);

			Assert.IsTrue(identityAccount.Entries == null || identityAccount.Entries.Count == 0);
		}

		[TestMethod]
		public async Task Engine_CreateFaucet()
		{
			const decimal FaucetAmount = 1000;

			await Engine.DefaultIdentity.WhenInitialized;

			var identity = Engine.DefaultIdentity;

			var faucetFlow = await Engine.ActivateFaucetIdentity(identity, null, FaucetAmount);

			Assert.AreEqual(1, OperationTaskflow.NumPending);

			await faucetFlow.WhenAcknowledged;

			var identityaccount = identity.Accounts[0];

			Assert.AreEqual(TokenStoreState.Changing, identityaccount.State);

			Assert.AreEqual(1, identityaccount.PendingChanges.Count);
			Assert.IsTrue(identityaccount.HasPendingChanges);
			Assert.IsTrue(identityaccount.Entries == null || identityaccount.Entries.Count == 0);

			var pending = identityaccount.PendingChanges[0];
			Assert.AreEqual(TokenStore.ChangeTopic.PendingTransfer, pending.Topic);
			Assert.AreEqual(FaucetAmount, pending.Amount);

			Simulation.CreateBlock();

			await faucetFlow.WhenCompleted;

			Assert.AreEqual(0, OperationTaskflow.NumPending);

			Assert.AreEqual(TokenStoreState.Online, identityaccount.State);

			Assert.AreEqual(FaucetAmount, identityaccount.Balance);

			Assert.AreEqual(0, identityaccount.PendingChanges.Count);
			Assert.IsFalse(identityaccount.HasPendingChanges);

			Assert.IsNotNull(identityaccount.Entries);
			Assert.AreEqual(1, identityaccount.Entries.Count);

			var entry = identityaccount.Entries[0];

			Assert.AreEqual(FaucetAmount, entry.Balance);

			Assert.IsNotNull(entry.Items);
			Assert.AreEqual(1, entry.Items.Count);

			var item = entry.Items[0];

			Assert.AreEqual(AccountEntryItemKind.Activation, item.Kind);
			Assert.AreEqual(FaucetAmount, item.Amount);
		}

		[TestMethod]
		public async Task Engine_Originate()
		{
			var source = Engine.DefaultIdentity;

			decimal transferAmount = 1;
			decimal networkFee = Engine.DefaultOperationFee;
			decimal expectedSourceBalance = source.Balance - transferAmount - networkFee;

			var flow = await Engine.CreateAccount("Account", source, source, transferAmount);

			await flow.WhenAcknowledged;
			await WhenMessagesDelivered();

			Assert.AreEqual(2, source.Accounts.Count);

			var account = source.Accounts[1];

			Assert.IsNotNull(account);

			Assert.AreEqual(TokenStoreState.Creating, account.State);

			// Account
			Assert.AreEqual(1, account.PendingChanges.Count);
			Assert.IsTrue(account.HasPendingChanges);
			Assert.IsTrue(account.Entries == null || account.Entries.Count == 0);

			var pending = account.PendingChanges[0];
			Assert.AreEqual(TokenStore.ChangeTopic.PendingTransfer, pending.Topic);
			Assert.AreEqual(transferAmount, pending.Amount);

			// Source
			Assert.AreEqual(1, source.PendingChanges.Count);
			Assert.IsTrue(source.HasPendingChanges);
			Assert.IsTrue(account.Entries == null || account.Entries.Count == 0);

			pending = source.PendingChanges[0];
			Assert.AreEqual(TokenStore.ChangeTopic.PendingTransfer, pending.Topic);
			Assert.AreEqual(-transferAmount - networkFee, pending.Amount);

			Simulation.CreateBlock();

			await flow.WhenCompleted;
			await WhenMessagesDelivered();

			Assert.AreEqual(TokenStoreState.Online, account.State);

			// Account
			Assert.AreEqual(transferAmount, account.Balance);

			Assert.AreEqual(0, account.PendingChanges.Count);
			Assert.IsFalse(account.HasPendingChanges);

			Assert.IsNotNull(account.Entries);
			Assert.AreEqual(1, account.Entries.Count);

			var entry = account.Entries[0];

			Assert.AreEqual(transferAmount, entry.Balance);

			Assert.IsNotNull(entry.Items);
			Assert.AreEqual(1, entry.Items.Count);

			var item = entry.Items[0];

			Assert.AreEqual(AccountEntryItemKind.Origination, item.Kind);
			Assert.AreEqual(transferAmount, item.Amount);

			// Source
			Assert.AreEqual(expectedSourceBalance, source.Balance);

			Assert.AreEqual(0, source.PendingChanges.Count);
			Assert.IsFalse(source.HasPendingChanges);

			Assert.IsNotNull(source.Entries);
			Assert.AreEqual(1, source.Entries.Count);

			entry = source.Entries[0];

			Assert.AreEqual(expectedSourceBalance, entry.Balance);

			Assert.IsNotNull(entry.Items);
			Assert.AreEqual(1, entry.Items.Count);

			item = entry.Items[0];

			Assert.AreEqual(AccountEntryItemKind.Origination, item.Kind);
			Assert.AreEqual(-transferAmount, item.Amount);
		}

		[TestMethod]
		public async Task Engine_Transfer()
		{
			await Engine.DefaultIdentity.WhenInitialized;

			decimal transferAmount = 2;
			decimal networkFee = Engine.DefaultOperationFee;

			var source = Engine.DefaultIdentity;

			await CreateDestinationAccount(transferAmount, source);

			var destination = source.Accounts[1];

			// Transfer
			decimal expectedSourceBalance = source.Balance - transferAmount - networkFee;
			decimal expectedDestinationBalance = destination.Balance + transferAmount;

			OperationTaskflow flow = await Engine.CommitTransfer(source, destination, transferAmount);

			await flow.WhenAcknowledged;
			await WhenMessagesDelivered();

			// Destination
			Assert.AreEqual(1, destination.PendingChanges.Count);
			Assert.IsTrue(destination.HasPendingChanges);
			Assert.AreEqual(1, destination.Entries.Count);

			var pending = destination.PendingChanges[0];
			Assert.AreEqual(TokenStore.ChangeTopic.PendingTransfer, pending.Topic);
			Assert.AreEqual(transferAmount, pending.Amount);

			// Source
			Assert.AreEqual(1, source.PendingChanges.Count);
			Assert.IsTrue(source.HasPendingChanges);
			Assert.IsTrue(destination.Entries.Count == 1);

			pending = source.PendingChanges[0];
			Assert.AreEqual(TokenStore.ChangeTopic.PendingTransfer, pending.Topic);
			Assert.AreEqual(-transferAmount - networkFee, pending.Amount);

			Simulation.CreateBlock();

			await flow.WhenCompleted;
			await WhenMessagesDelivered();

			// Destination
			Assert.AreEqual(expectedDestinationBalance, destination.Balance);

			Assert.AreEqual(0, destination.PendingChanges.Count);
			Assert.IsFalse(destination.HasPendingChanges);

			Assert.IsNotNull(destination.Entries);
			Assert.AreEqual(2, destination.Entries.Count);

			var entry = destination.Entries[1];

			Assert.AreEqual(expectedDestinationBalance, entry.Balance);

			Assert.IsNotNull(entry.Items);
			Assert.AreEqual(1, entry.Items.Count);

			var item = entry.Items[0];

			Assert.AreEqual(AccountEntryItemKind.Transfer, item.Kind);
			Assert.AreEqual(transferAmount, item.Amount);

			// Source
			Assert.AreEqual(expectedSourceBalance, source.Balance);

			Assert.AreEqual(0, source.PendingChanges.Count);
			Assert.IsFalse(source.HasPendingChanges);

			Assert.IsNotNull(source.Entries);
			Assert.AreEqual(2, source.Entries.Count);

			entry = source.Entries[1];

			Assert.AreEqual(expectedSourceBalance, entry.Balance);

			Assert.IsNotNull(entry.Items);
			Assert.AreEqual(1, entry.Items.Count);

			item = entry.Items[0];

			Assert.AreEqual(AccountEntryItemKind.Transfer, item.Kind);
			Assert.AreEqual(-transferAmount, item.Amount);
		}

		[TestMethod]
		public async Task Engine_TimeoutFaucet()
		{
			const decimal FaucetAmount = 1000;

			await Engine.DefaultIdentity.WhenInitialized;

			var identity = Engine.DefaultIdentity;

			var flow = await Engine.ActivateFaucetIdentity(identity, null, FaucetAmount);

			await flow.WhenAcknowledged;

			Simulation.Timeout(flow.Task);

			await flow.WhenCompleted;

			Assert.IsFalse(identity.HasPendingChanges);
		}

		[TestMethod]
		public async Task Engine_TimeoutTransfer()
		{
			await Engine.DefaultIdentity.WhenInitialized;

			decimal transferAmount = 1;

			var source = Engine.DefaultIdentity;

			await CreateDestinationAccount(transferAmount, source);

			var destination = source.Accounts[1];

			// Transfer
			OperationTaskflow flow = await Engine.CommitTransfer(source, destination, transferAmount);

			await flow.WhenAcknowledged;

			Simulation.Timeout(flow.Task);

			await flow.WhenCompleted;
			await WhenMessagesDelivered();

			// Destination
			Assert.IsFalse(destination.HasPendingChanges);

			// Source
			Assert.IsFalse(source.HasPendingChanges);
		}

		[TestMethod]
		public async Task Engine_TimeoutOriginate()
		{
			decimal transferAmount = 1;

			var source = Engine.DefaultIdentity;

			// Create destination account
			var flow = await Engine.CreateAccount("Account", source, source, transferAmount);

			await flow.WhenAcknowledged;

			Simulation.Timeout(flow.Task);

			await flow.WhenCompleted;
			await WhenMessagesDelivered();

			Assert.AreEqual(2, source.Accounts.Count);

			var destination = source.Accounts[1];

			Assert.AreEqual(TokenStoreState.UnheardOf, destination.State);

			Assert.IsFalse(destination.HasPendingChanges);
			Assert.IsFalse(source.HasPendingChanges);
		}

		private async Task CreateDestinationAccount(decimal transferAmount, Identity source)
		{
			// Create destination account
			var flow = await Engine.CreateAccount("Destination", source, source, transferAmount);
			await flow.WhenAcknowledged;
			await WhenMessagesDelivered();
			Simulation.CreateBlock();
			await flow.WhenCompleted;
			await WhenMessagesDelivered();
		}
	}
}