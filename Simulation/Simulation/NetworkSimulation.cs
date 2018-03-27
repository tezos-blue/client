using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SLD.Tezos.Simulation
{
	using Blockchain;
	using Client.Model;
	using Notifications;
	using Protocol;
	using SLD.Tezos.Client.Connections;

	public partial class NetworkSimulation : SimulationObject
	{
		public NetworkSimulation(SimulationParameters parameters = null) : base(parameters)
		{
			Parameters.Simulation = this;

			Hub = new NotificationHub(Parameters);

			InitializeBlockchain();
		}

		#region Connections

		public ConnectionEndpoint RegisterConnection(string instanceID)
		{
			return Hub.Register(instanceID);
		}

		public SimulatedConnection Connect(string instanceID)
		{
			RegisterConnection(instanceID);

			return new SimulatedConnection(Parameters);
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
					var account = GetAccount(accountID) as ISimulatedTokenStore;

					Hub.MonitorAccount(instanceID, account);
				}
			}
		}

		private void MonitorAccount(string instanceID, SimulatedAccount account)
		{
			Hub.MonitorAccount(instanceID, account);
		}

		#endregion Notifications

		#region Identities

		public List<SimulatedIdentity> Identities = new List<SimulatedIdentity>();

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
			var account = GetAccount(accountID) as ISimulatedTokenStore;

			account.SetBalance(balance);
		}

		internal AccountInfo GetAccountInfo(string accountID)
		{
			TokenStore account = GetAccount(accountID);

			return new AccountInfo
			{
				AccountID = account.AccountID,
				ManagerID = account.Manager.AccountID,
				Balance = account.Balance,
			};
		}

		internal decimal GetBalance(string accountID)
		{
			TokenStore account = GetAccount(accountID);

			return account.Balance;
		}

		internal TokenStore GetAccount(string accountID, bool liveOnly = true)
		{
			TokenStore account = Accounts.FirstOrDefault(a => a.AccountID == accountID);

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

			var account = new SimulatedAccount(this, manager, 0);
			Accounts.Add(account);

			task.AccountID = account.AccountID;

			var source = GetAccount(task.SourceID);

			source.Notify(new TransactionPendingEvent
			{
				AccountID = source.AccountID,
				Amount = -task.TotalAmount,
				ContraAccountID = account.AccountID,
				OperationID = task.OperationID,
			});

			manager.Notify(new OriginatePendingEvent
			{
				Name = task.Name,
				ManagerID = task.ManagerID,
				AccountID = account.AccountID,
				Amount = task.TransferAmount,
				ContraAccountID = source.AccountID,
				OperationID = task.OperationID,
			});

			blockchain.Add(task);

			return task;
		}

		internal CreateFaucetTask AlphaCreateFaucet(CreateFaucetTask task, string connectionID)
		{
			var manager = GetIdentity(task.ManagerID);

			var account = new SimulatedAccount(this, manager, FaucetAmount);

			Accounts.Add(account);

			task.AccountID = account.AccountID;
			task.TransferAmount = FaucetAmount;
			task.OperationID = CreateOperationID();
			task.Client = new ClientInfo
			{
				InstanceID = connectionID,
			};

			blockchain.Add(task);

			manager.Notify(new OriginatePendingEvent
			{
				AccountID = account.AccountID,
				Amount = FaucetAmount,
				ManagerID = task.ManagerID,
				Name = task.Name,
				OperationID = task.OperationID,
			});

			return task;
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

			var notify = Task.Run(async () =>
			{
				var source = GetAccount(task.SourceID);
				var destination = GetAccount(task.DestinationID);

				source.Notify(new TransactionPendingEvent
				{
					AccountID = source.AccountID,
					Amount = -task.TotalAmount,
					ContraAccountID = destination.AccountID,
					OperationID = task.OperationID,
				});

				destination.Notify(new TransactionPendingEvent
				{
					AccountID = destination.AccountID,
					Amount = task.TransferAmount,
					ContraAccountID = source.AccountID,
					OperationID = task.OperationID,
				});
			});

			return task;
		}

		#endregion Transactions

		#region Timeout

		internal void Timeout(ProtectedTask task)
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

				default:
					break;
			}
		}

		#endregion Timeout

		#region Blockchain

		private static int NextOperationID = 0;
		private SimulatedBlockchain blockchain;

		public void CreateBlock()
		{
			var block = blockchain.CreateBlock();

			OnBlockCreated(block);
		}

		private string CreateOperationID()
		{
			return $"op#{NextOperationID++}";
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
				switch (task)
				{
					case CreateFaucetTask taskFaucet:
						{
							// Update state
							var destination = GetAccount(taskFaucet.AccountID, false) as SimulatedAccount;
							destination.UpdateBalance(taskFaucet.TransferAmount);

							// Register
							MonitorAccount(taskFaucet.Client.InstanceID, destination);

							// Add entry
							var entry = new AccountEntry(block.Index, block.Time)
							{
								Balance = destination.Balance,
								OperationID = taskFaucet.OperationID,
								Items = new AccountEntryItem[]
								{
									new AccountEntryItem
									{
										Kind = AccountEntryItemKind.Origination,
										Amount = FaucetAmount,
									},
								}
								.ToList(),
							};

							destination.Entries.Add(entry);

							// Notify client
							var identity = GetAccount(taskFaucet.ManagerID);

							identity.Notify(new OriginateEvent
							{
								Name = taskFaucet.Name,
								ManagerID = taskFaucet.ManagerID,
								AccountID = destination.AccountID,
								Balance = destination.Balance,
								OperationID = taskFaucet.OperationID,
								Entry = entry,
							});
						}
						break;

					case CreateContractTask taskOriginate:
						{
							// Update state
							var source = GetAccount(taskOriginate.SourceID);
							var sourceBalance = source.Balance - taskOriginate.TotalAmount;

							source.UpdateBalance(sourceBalance);

							var destination = GetAccount(taskOriginate.AccountID, false) as SimulatedAccount;
							destination.UpdateBalance(taskOriginate.TransferAmount);

							// Register
							MonitorAccount(taskOriginate.Client.InstanceID, destination);

							// Add entries
							var sourceEntry = new AccountEntry(block.Index, block.Time)
							{
								Balance = sourceBalance,
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
							source.Notify(new BalanceChangedEvent
							{
								AccountID = source.AccountID,
								Balance = source.Balance,
								OperationID = taskOriginate.OperationID,
								Entry = sourceEntry,
							});

							var manager = GetIdentity(taskOriginate.ManagerID);

							manager.Notify(new OriginateEvent
							{
								Name = taskOriginate.Name,
								ManagerID = taskOriginate.ManagerID,
								AccountID = destination.AccountID,
								Balance = destination.Balance,
								OperationID = taskOriginate.OperationID,
								Entry = destinationEntry,
							});
						}
						break;

					case TransferTask taskTransfer:
						{
							//Update state
							var source = GetAccount(taskTransfer.SourceID);
							source.UpdateBalance(source.Balance - taskTransfer.TransferAmount - taskTransfer.NetworkFee);

							var destination = GetAccount(taskTransfer.DestinationID, false);
							destination.UpdateBalance(destination.Balance + taskTransfer.TransferAmount);

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
							source.Notify(new BalanceChangedEvent
							{
								AccountID = source.AccountID,
								Balance = source.Balance,
								OperationID = taskTransfer.OperationID,
								Entry = sourceEntry,
							});

							destination.Notify(new BalanceChangedEvent
							{
								AccountID = destination.AccountID,
								Balance = destination.Balance,
								OperationID = taskTransfer.OperationID,
								Entry = destinationEntry,
							});
						}
						break;

					default:
						throw new NotImplementedException();
				}
			}
		}

		#endregion Blockchain

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