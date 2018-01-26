using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SLD.Tezos.Simulation
{
	using Blockchain;
	using Client.Model;
	using Protocol;

	public partial class NetworkSimulation
	{
		private static int NextID = 1;

		public NetworkSimulation(SimulationParameters parameters = null)
		{
			Parameters = parameters ?? new SimulationParameters();
			Parameters.Simulation = this;

			if (!Parameters.StartFresh)
			{
				MyIdentity = new SimulatedIdentity("FirstIdentity", Accounts)
				{
					Name = $"Identity {NextID++}",
				};

				Identities.AddRange(new[]
				{
					MyIdentity,
				});

				MyIdentity.UpdateBalance(1000);

				var account = new SimulatedAccount(MyIdentity, FaucetAmount)
				{
					Name = "Faucet",
				};

				Accounts.Add(account);
				MyIdentity.AddAccount(account);
			}

			InitializeBlockchain();
		}

		public SimulationParameters Parameters { get; private set; }

		#region Connections

		private Dictionary<string, ConnectionEndpoint> connections = new Dictionary<string, ConnectionEndpoint>();

		internal ConnectionEndpoint RegisterConnection(string connectionID)
		{
			Debug.Assert(!connections.ContainsKey(connectionID));

			var endpoint = new ConnectionEndpoint
			{
				ID = connectionID,
			};

			connections.Add(connectionID, endpoint);

			return endpoint;
		}

		internal void UnregisterConnection(string connectionID)
		{
			//Debug.Assert(connections.ContainsKey(connectionID));
			connections.Remove(connectionID);
		}

		internal async Task MonitorAccounts(string connectionID, params string[] accountIDs)
		{
			if (accountIDs != null && accountIDs.Any())
			{
				var connection = connections[connectionID];

				connection.MonitoredAccountIDs.AddRange(accountIDs);

				foreach (var accountID in accountIDs)
				{
					var account = await GetAccount(accountID) as IEventSource;

					account.Listeners.Add(connection);
				}
			}
		}

		private void MonitorAccount(string connectionID, SimulatedAccount account)
		{
			var connection = connections[connectionID];
			connection.MonitoredAccountIDs.Add(account.AccountID);
			(account as IEventSource).Listeners.Add(connection);
		}

		#endregion Connections

		#region Identities

		public List<SimulatedIdentity> Identities = new List<SimulatedIdentity>();
		public SimulatedIdentity MyIdentity;

		private SimulatedIdentity FindIdentity(string identityID)
			=> Identities.FirstOrDefault(i => i.AccountID == identityID);

		#endregion Identities

		#region Accounts

		public List<SimulatedAccount> Accounts = new List<SimulatedAccount>();

		public async Task<RegisterIdentityTask> RegisterIdentity(RegisterIdentityTask task)
		{
			var identityID = task.IdentityID;

			// Return info in any case, even if the identity has not been seen before
			var info = new IdentityInfo
			{
				AccountID = identityID,
			};

			// Register for identity notifications
			await MonitorAccounts(task.Client.InstanceID, identityID);

			// Find identity in database
			var identity = FindIdentity(identityID);

			if (identity != null)
			{
				info.Balance = await GetBalance(identityID);
				info.Name = identity.Name;
				info.Stereotype = identity.Stereotype;

				// Add managed accounts to info
				var accountTasks = identity.ManagedAccounts.Select(
					async account => new IdentityAccountInfo
					{
						AccountID = account.AccountID,
						Name = account.Name,
						Stereotype = account.Stereotype,
						Balance = await GetBalance(account.AccountID),
					});

				await Task.WhenAll(accountTasks);

				info.Accounts = accountTasks.Select(t => t.Result).ToArray();

				// Register for account notifications
				await MonitorAccounts(task.Client.InstanceID, info.Accounts.Select(a => a.AccountID).ToArray());
			}

			// All the info we have
			task.Info = info;
			task.Progress = TaskProgress.Confirmed;

			return task;
		}

		internal async Task<AccountInfo> GetAccountInfo(string accountID)
		{
			TokenStore account = await GetAccount(accountID);

			return new AccountInfo
			{
				AccountID = account.AccountID,
				ManagerID = account.Manager.AccountID,
				Balance = account.Balance,
			};
		}

		internal async Task<decimal> GetBalance(string accountID)
		{
			TokenStore account = await GetAccount(accountID);

			return account.Balance;
		}

		private async Task<TokenStore> GetAccount(string accountID, bool liveOnly = true)
		{
			await Task.Delay(50);

			TokenStore account = Accounts.FirstOrDefault(a => a.AccountID == accountID);

			if (account == null)
			{
				account = Identities.FirstOrDefault(i => i.AccountID == accountID);
			}

			if (account == null)
			{
				// Assume, it is an identity
				var identity = new SimulatedIdentity(accountID);
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

		internal async Task<CreateFaucetTask> AlphaCreateFaucet(CreateFaucetTask task, string connectionID)
		{
			await Task.Delay(50);

			var manager = GetIdentity(task.ManagerID);

			var account = new SimulatedAccount(manager, FaucetAmount);

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

		internal async Task<CreateContractTask> PrepareCreateContract(CreateContractTask task)
		{
			await Task.Delay(50);

			task.Operation = "CCCCCC";
			task.Progress = TaskProgress.Prepared;

			return task;
		}

		internal async Task<CreateContractTask> CreateContract(CreateContractTask task, string connectionID)
		{
			await Task.Delay(50);

			task.OperationID = CreateOperationID();
			task.Client = new ClientInfo
			{
				InstanceID = connectionID,
			};

			var manager = GetIdentity(task.ManagerID);

			var account = new SimulatedAccount(manager, 0);
			Accounts.Add(account);

			task.AccountID = account.AccountID;

			var source = await GetAccount(task.SourceID);

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

		private SimulatedIdentity GetIdentity(string identityID)
		{
			var identity = Identities.FirstOrDefault(i => i.ManagerID == identityID);

			if (identity == null)
			{
				// Assume, it is an identity
				identity = new SimulatedIdentity(identityID);
				Identities.Add(identity);
			}

			return identity;
		}

		#endregion Origination

		#region Transactions

		internal async Task<TransferTask> PrepareTransfer(TransferTask task)
		{
			await Task.Delay(50);

			task.Operation = "FFFFFF";
			task.Progress = TaskProgress.Prepared;

			return task;
		}

		internal async Task<TransferTask> CommitTransfer(TransferTask task)
		{
			await Task.Delay(50);

			task.OperationID = CreateOperationID();

			blockchain.Add(task);

			var notify = Task.Run(async () =>
			{
				var source = await GetAccount(task.SourceID);
				var destination = await GetAccount(task.DestinationID);

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

		internal async Task Timeout(ProtectedTask task)
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
						await GetAccount(pendingOriginate.SourceID, false) :
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
					var destination = await GetAccount(pendingTransfer.DestinationID);

					destination.Notify(new TransactionTimeoutEvent
					{
						OperationID = pendingTransfer.OperationID,
						AccountID = pendingTransfer.DestinationID,
					});

					source = await GetAccount(pendingTransfer.SourceID, false);

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

		internal async Task CreateBlock()
		{
			var block = blockchain.CreateBlock();

			await OnBlockCreated(block);
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

		private async Task OnBlockCreated(Blockchain.Block block)
		{
			foreach (var task in block.Operations)
			{
				switch (task)
				{
					case CreateFaucetTask taskFaucet:
						{
							// Update state
							var destination = await GetAccount(taskFaucet.AccountID, false) as SimulatedAccount;
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
							var identity = await GetAccount(taskFaucet.ManagerID);

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
							var source = await GetAccount(taskOriginate.SourceID);
							var sourceBalance = source.Balance - taskOriginate.TotalAmount;

							source.UpdateBalance(sourceBalance);

							var destination = await GetAccount(taskOriginate.AccountID, false) as SimulatedAccount;
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
							var source = await GetAccount(taskTransfer.SourceID);
							source.UpdateBalance(source.Balance - taskTransfer.TransferAmount - taskTransfer.NetworkFee);

							var destination = await GetAccount(taskTransfer.DestinationID, false);
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