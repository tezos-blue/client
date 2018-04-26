using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SLD.Tezos.Simulation
{
	using Blockchain;
	using Client.Connections;
	using Protocol;
	using Services;

	public partial class NetworkSimulation : SimulationObject
	{
		public NetworkSimulation(SimulationParameters parameters = null) : base(parameters)
		{
			Parameters.Simulation = this;

			Hub = new NotificationHub(Parameters);

			InitializeBlockchain();

			//InitializeTestModel();
		}

		#region Test Model

		public SimulatedIdentity TestIdentity { get; private set; }

		public (SimulatedTokenStore, SimulatedTokenStore) TestSourceAndDestination
			=> (TestIdentity, TestIdentity.Accounts[0]);

		private void InitializeTestModel()
		{
			TestIdentity = GetIdentity(nameof(TestIdentity));

			// Default source is identity account
			TestIdentity.Balance = 1000;

			// Default destination
			CreateAccount(TestIdentity, 0);
		}

		#endregion Test Model

		#region Connections

		public ConnectionEndpoint RegisterConnection(string instanceID)
		{
			return Hub.Register(instanceID);
		}

		public SimulatedConnection Connect(string instanceID)
		{
			return new SimulatedConnection(Parameters)
			{
				InstanceID = instanceID,
			};
		}

		internal void UnregisterConnection(string instanceID)
		{
			Hub.Unregister(instanceID);
		}

		#endregion Connections

		#region Notifications

		public NotificationHub Hub { get; private set; }

		internal void MonitorAccounts(string instanceID, params string[] accountIDs)
		{
			Debug.Assert(accountIDs != null);

			if (accountIDs.Any())
			{
				foreach (var accountID in accountIDs)
				{
					var account = GetAccount(accountID) as SimulatedTokenStore;

					Hub.MonitorAccount(instanceID, account);
				}
			}
		}

		private void MonitorAccount(string instanceID, SimulatedTokenStore account)
		{
			Hub.MonitorAccount(instanceID, account);
		}

		#endregion Notifications

		#region Cache

		private Cache Cache = new Cache();

		#endregion Cache

		#region Identities

		public List<SimulatedIdentity> Identities = new List<SimulatedIdentity>();

		internal ActivateIdentityTask ActivateIdentity(ActivateIdentityTask task, string connectionID)
		{
			var identity = GetIdentity(task.IdentityID);

			task.OperationID = CreateOperationID();
			task.Client = new ClientInfo
			{
				InstanceID = connectionID,
			};

			blockchain.Add(task);

			var sourceEvent = new ActivationPendingEvent
			{
				IdentityID = identity.AccountID,
				Amount = task.Amount,
				OperationID = task.OperationID,
			};

			identity.Notify(sourceEvent);

			Cache.Store(task, sourceEvent, null);

			return task;
		}

		private SimulatedIdentity FindIdentity(string identityID)
					=> Identities.FirstOrDefault(i => i.AccountID == identityID);

		private SimulatedIdentity GetIdentity(string identityID)
		{
			var identity = Identities.FirstOrDefault(i => i.ManagerID == identityID);

			if (identity == null)
			{
				// Assume, it is an identity
				identity = new SimulatedIdentity(this, identityID);
				Identities.Add(identity);
			}

			return identity;
		}

		#endregion Identities

		#region Accounts

		public List<SimulatedAccount> Accounts = new List<SimulatedAccount>();

		public RegisterIdentityTask RegisterIdentity(RegisterIdentityTask task)
		{
			var identityID = task.IdentityID;

			Trace($"Register identity: {identityID}");

			// Return info in any case, even if the identity has not been seen before
			var info = new IdentityInfo
			{
				AccountID = identityID,
			};

			// Register for identity notifications
			MonitorAccounts(task.Client.InstanceID, identityID);

			// Find identity in database
			var identity = FindIdentity(identityID);

			if (identity != null)
			{
				info.Balance = GetBalance(identityID);
				info.Name = identity.Name;
				info.Stereotype = identity.Stereotype;

				// Add managed accounts to info
				info.Accounts = identity.ManagedAccounts.Select(
					account => new IdentityAccountInfo
					{
						AccountID = account.AccountID,
						Name = account.Name,
						Stereotype = account.Stereotype,
						Balance = GetBalance(account.AccountID),
					})
					.ToArray();

				// Register for account notifications
				MonitorAccounts(task.Client.InstanceID, info.Accounts.Select(a => a.AccountID).ToArray());
			}

			// All the info we have
			task.Info = info;
			task.Progress = TaskProgress.Confirmed;

			return task;
		}

		public void SetBalance(string accountID, decimal balance)
		{
			var account = GetAccount(accountID);

			account.Balance = balance;
		}

		public void RemoveStaleAccount(string accountID, string managerID)
		{
			var identity = FindIdentity(managerID);

			if (identity == null)
				throw new ArgumentException("Identity not found", nameof(managerID));

			var found = identity.Accounts.FirstOrDefault(account => account.AccountID == accountID);

			if (found == null)
				throw new ArgumentException("Account not found", nameof(accountID));

			identity.Accounts.Remove(found);
		}

		internal AccountInfo GetAccountInfo(string accountID)
		{
			var account = GetAccount(accountID);

			return new AccountInfo
			{
				AccountID = account.AccountID,
				ManagerID = account.Manager.AccountID,
				Balance = account.Balance,
			};
		}

		internal decimal GetBalance(string accountID)
		{
			var account = GetAccount(accountID);

			return account.Balance;
		}

		internal SimulatedTokenStore GetAccount(string accountID, bool liveOnly = true)
		{
			SimulatedTokenStore account = Accounts.FirstOrDefault(a => a.AccountID == accountID);

			if (account == null)
			{
				account = Identities.FirstOrDefault(i => i.AccountID == accountID);
			}

			if (account == null)
			{
				// Assume, it is an identity
				var identity = new SimulatedIdentity(this, accountID);
				Identities.Add(identity);

				account = identity;
			}
			else
			{
				// Check if account is alive
				if (liveOnly && !account.IsLive)
				{
					throw new ServerException(ServerError.AccountNotFound, $"Not in simulated Network: {accountID}", null);
				}
			}

			return account;
		}

		#endregion Accounts

		#region Origination

		public CreateContractTask PrepareCreateContract(CreateContractTask task)
		{
			task.Operation = "CCCCCC";
			task.Progress = TaskProgress.Prepared;

			return task;
		}

		public CreateContractTask CreateContract(CreateContractTask task, string connectionID)
		{
			task.OperationID = CreateOperationID();
			task.Client = new ClientInfo
			{
				InstanceID = connectionID,
			};

			var manager = GetIdentity(task.ManagerID);

			SimulatedAccount account = CreateAccount(manager, 0);

			task.AccountID = account.AccountID;

			blockchain.Add(task);

			var source = GetAccount(task.SourceID);

			var sourceEvent = new TransactionPendingEvent
			{
				AccountID = source.AccountID,
				Amount = -task.TotalAmount,
				ContraAccountID = account.AccountID,
				OperationID = task.OperationID,
			};

			source.Notify(sourceEvent);

			var destinationEvent = new OriginatePendingEvent
			{
				Name = task.Name,
				ManagerID = task.ManagerID,
				AccountID = account.AccountID,
				Amount = task.TransferAmount,
				ContraAccountID = source.AccountID,
				OperationID = task.OperationID,
			};

			manager.Notify(destinationEvent);

			Cache.Store(task, sourceEvent, destinationEvent);

			return task;
		}

		private SimulatedAccount CreateAccount(SimulatedIdentity manager, decimal balance)
		{
			var account = new SimulatedAccount(this, manager, balance);

			Accounts.Add(account);
			manager.Accounts.Add(account);

			return account;
		}

		#endregion Origination

		#region Transactions

		public TransferTask PrepareTransfer(TransferTask task)
		{
			task.Operation = "FFFFFF";
			task.Progress = TaskProgress.Prepared;

			return task;
		}

		public TransferTask CommitTransfer(TransferTask task)
		{
			task.OperationID = CreateOperationID();

			blockchain.Add(task);

			var source = GetAccount(task.SourceID);
			var destination = GetAccount(task.DestinationID);

			var sourceEvent = new TransactionPendingEvent
			{
				AccountID = source.AccountID,
				Amount = -task.TotalAmount,
				ContraAccountID = destination.AccountID,
				OperationID = task.OperationID,
			};

			source.Notify(sourceEvent);

			var destinationEvent = new TransactionPendingEvent
			{
				AccountID = destination.AccountID,
				Amount = task.TransferAmount,
				ContraAccountID = source.AccountID,
				OperationID = task.OperationID,
			};

			destination.Notify(destinationEvent);

			Cache.Store(task, sourceEvent, destinationEvent);

			return task;
		}

		#endregion Transactions

		#region Timeout

		public void Timeout(OperationTask task)
		{
			switch (task)
			{
				case CreateContractTask pendingOriginate:

					// Notify clients
					var manager = GetIdentity(pendingOriginate.ManagerID);

					manager.Notify(new OriginationTimeoutEvent
					{
						OperationID = pendingOriginate.OperationID,
						ManagerID = pendingOriginate.ManagerID,
						AccountID = pendingOriginate.AccountID,
					});

					var source = pendingOriginate.SourceID != null ?
						GetAccount(pendingOriginate.SourceID, false) :
						null;

					if (source != null)
					{
						source.Notify(new TransactionTimeoutEvent
						{
							OperationID = pendingOriginate.OperationID,
							AccountID = pendingOriginate.SourceID,
						});
					}

					break;

				case TransferTask pendingTransfer:
					// Notify clients
					var destination = GetAccount(pendingTransfer.DestinationID);

					destination.Notify(new TransactionTimeoutEvent
					{
						OperationID = pendingTransfer.OperationID,
						AccountID = pendingTransfer.DestinationID,
					});

					source = GetAccount(pendingTransfer.SourceID, false);

					source.Notify(new TransactionTimeoutEvent
					{
						OperationID = pendingTransfer.OperationID,
						AccountID = pendingTransfer.SourceID,
					});

					break;

				case ActivateIdentityTask pendingActivate:
					// Notify clients
					var identity = GetAccount(pendingActivate.IdentityID);

					identity.Notify(new ActivationTimeoutEvent
					{
						IdentityID = pendingActivate.IdentityID,
						OperationID = pendingActivate.OperationID,
					});

					break;

				default:
					break;
			}
		}

		#endregion Timeout

		#region Blockchain

		private SimulatedBlockchain blockchain;

		public void CreateBlock()
		{
			var block = blockchain.CreateBlock();

			OnBlockCreated(block);
		}

		private void InitializeBlockchain()
		{
			blockchain = new SimulatedBlockchain(Parameters);

			blockchain.BlockCreated += block => Task.Run(() => OnBlockCreated(block));
		}

		private void OnBlockCreated(Block block)
		{
			foreach (var task in block.Operations)
			{
				OperationEvent sourceEvent = null, destinationEvent = null;

				switch (task)
				{
					case CreateContractTask taskOriginate:
						{
							// Update state
							var source = GetAccount(taskOriginate.SourceID);

							source.Balance -= taskOriginate.TotalAmount;

							var destination = GetAccount(taskOriginate.AccountID, false);
							destination.Balance = taskOriginate.TransferAmount;

							// Register
							MonitorAccount(taskOriginate.Client.InstanceID, destination);

							// Add entries
							var sourceEntry = new AccountEntry(block.Index, block.Time)
							{
								Balance = source.Balance,
								OperationID = taskOriginate.OperationID,
								NetworkFee = taskOriginate.NetworkFee,
								StorageFee = 1,
								Items = new AccountEntryItem[]
								{
									new AccountEntryItem
									{
										Kind = AccountEntryItemKind.Origination,
										Amount = -taskOriginate.TransferAmount,
										ContraAccountID = taskOriginate.AccountID,
									},
								}
								.ToList(),
							};

							source.Entries.Add(sourceEntry);

							var destinationEntry = new AccountEntry(block.Index, block.Time)
							{
								Balance = destination.Balance,
								OperationID = taskOriginate.OperationID,
								Items = new AccountEntryItem[]
								{
									new AccountEntryItem
									{
										Kind = AccountEntryItemKind.Origination,
										Amount = taskOriginate.TransferAmount,
										ContraAccountID = taskOriginate.SourceID,
									},
								}
								.ToList(),
							};

							destination.Entries.Add(destinationEntry);

							// Notify clients

							sourceEvent = new BalanceChangedEvent
							{
								AccountID = source.AccountID,
								Balance = source.Balance,
								OperationID = taskOriginate.OperationID,
								Entry = sourceEntry,
								State = source.IsLive ? AccountState.Live : AccountState.Archived,
							};

							source.Notify(sourceEvent);

							var manager = GetIdentity(taskOriginate.ManagerID);

							destinationEvent = new OriginateEvent
							{
								Name = taskOriginate.Name,
								ManagerID = taskOriginate.ManagerID,
								AccountID = destination.AccountID,
								Balance = destination.Balance,
								OperationID = taskOriginate.OperationID,
								Entry = destinationEntry,
								State = AccountState.Live,
							};

							manager.Notify(destinationEvent);
						}
						break;

					case TransferTask taskTransfer:
						{
							//Update state
							var source = GetAccount(taskTransfer.SourceID);
							source.Balance -= taskTransfer.TotalAmount;

							var destination = GetAccount(taskTransfer.DestinationID, false);
							destination.Balance += taskTransfer.TransferAmount;

							// Add entries
							var sourceEntry = new AccountEntry(block.Index, block.Time)
							{
								Balance = source.Balance,
								OperationID = taskTransfer.OperationID,
								NetworkFee = taskTransfer.NetworkFee,
								Items = new AccountEntryItem[]
								{
									new AccountEntryItem
									{
										Kind = AccountEntryItemKind.Transfer,
										Amount = -taskTransfer.TransferAmount,
										ContraAccountID = taskTransfer.DestinationID,
									},
								}
								.ToList(),
							};

							source.Entries.Add(sourceEntry);

							var destinationEntry = new AccountEntry(block.Index, block.Time)
							{
								Balance = destination.Balance,
								OperationID = taskTransfer.OperationID,
								Items = new AccountEntryItem[]
								{
									new AccountEntryItem
									{
										Kind = AccountEntryItemKind.Transfer,
										Amount = taskTransfer.TransferAmount,
										ContraAccountID = taskTransfer.SourceID,
									},
								}
								.ToList(),
							};

							destination.Entries.Add(destinationEntry);

							// Notify clients
							sourceEvent = new BalanceChangedEvent
							{
								AccountID = source.AccountID,
								Balance = source.Balance,
								OperationID = taskTransfer.OperationID,
								Entry = sourceEntry,
								State = source.IsLive ? AccountState.Live : AccountState.Archived,
							};

							source.Notify(sourceEvent);

							destination.Notify(new BalanceChangedEvent
							{
								AccountID = destination.AccountID,
								Balance = destination.Balance,
								OperationID = taskTransfer.OperationID,
								Entry = destinationEntry,
								State = AccountState.Live,
							});
						}
						break;

					case ActivateIdentityTask taskActivate:
						{
							//Update state
							var source = GetAccount(taskActivate.IdentityID, true);
							source.Balance += taskActivate.Amount;

							// Add entries
							var sourceEntry = new AccountEntry(block.Index, block.Time)
							{
								Balance = source.Balance,
								OperationID = taskActivate.OperationID,
								Items = new AccountEntryItem[]
								{
									new AccountEntryItem
									{
										Kind = AccountEntryItemKind.Activation,
										Amount = taskActivate.Amount,
									},
								}
								.ToList(),
							};

							source.Entries.Add(sourceEntry);

							// Notify clients
							sourceEvent = new BalanceChangedEvent
							{
								AccountID = source.AccountID,
								Balance = source.Balance,
								OperationID = taskActivate.OperationID,
								Entry = sourceEntry,
								State = AccountState.Live,
							};

							source.Notify(sourceEvent);
						}
						break;

					default:
						throw new NotImplementedException();
				}

				Cache.Update(task, TaskProgress.Confirmed, sourceEvent, destinationEvent);
			}
		}

		#endregion Blockchain

		#region Operations

		private int NextOperationID = 0;

		internal OperationStatus GetOperationStatus(OperationTask task)
		{
			var info = Cache.GetOperation(task.OperationID);

			if (info.Progress == task.Progress)
			{
				// Nothing changed
				return new OperationStatus
				{
					OperationID = task.OperationID,
					RetryAfter = TimeSpan.FromMilliseconds(300),
				};
			}
			else
			{
				// Progress has changed
				return new OperationStatus
				{
					OperationID = task.OperationID,
					SourceEvent = info.LastSourceEvent,
					DestinationEvent = info.LastDestinationEvent,
				};
			}
		}

		private string CreateOperationID()
		{
			return $"op#{NextOperationID++}";
		}

		#endregion Operations

		#region Helpers

		private static int NextSimulationNo = 1;
		private int SimulationNo = NextSimulationNo++;

		public override string ToString()
		{
			return $"Simulation {SimulationNo}";
		}

		#endregion Helpers
	}
}