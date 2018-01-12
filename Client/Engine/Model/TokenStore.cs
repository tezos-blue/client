﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Model
{
	using Connections;
	using Protocol;

	public enum TokenStoreState
	{
		Uninitialized,
		Creating,
		Initializing,
		Online,
		Offline,
		Unknown,
		Changing
	}

	public abstract class TokenStore : ClientObject
	{
		public abstract string AccountID { get; }

		public string Stereotype { get; internal set; }

		#region Name

		private string _Name;

		public string Name
		{
			get
			{
				return _Name ?? AccountID;
			}

			set
			{
				if (_Name != value)
				{
					_Name = value;
					FirePropertyChanged("Name");
				}
			}
		}

		#endregion Name

		#region State

		private TokenStoreState _State;

		public TokenStoreState State
		{
			get
			{
				return _State;
			}

			internal set
			{
				if (_State != value)
				{
					_State = value;
					FirePropertyChanged();
					FirePropertyChanged("IsChanging");
				}
			}
		}

		public bool IsChanging => State == TokenStoreState.Changing || State == TokenStoreState.Creating;

		#endregion State

		#region Manager

		protected string storedManagerID;

		public abstract Identity Manager { get; }

		public string ManagerID => Manager == null ? storedManagerID : Manager.AccountID;

		#endregion Manager

		public abstract bool IsLive { get; }

		public override string ToString()
		{
			return AccountID;
		}

		internal virtual Task Initialize(Connection connection)
		{
			State = TokenStoreState.Initializing;

			return Task.Run(async () =>
			{
				try
				{
					var info = await connection.GetAccountInfo(AccountID);

					UpdateBalance(info.Balance);

					State = TokenStoreState.Online;
				}
				catch (ServerException e)
				{
					Trace(e);

					switch (e.ServerError)
					{
						case ServerError.AccountNotFound:
							State = TokenStoreState.Unknown;
							break;

						default:
							State = TokenStoreState.Offline;
							break;
					}

					return;
				}
				catch (Exception e)
				{
					Trace(e);
					return;
				}
			});
		}

		#region Balance

		private decimal _Balance;

		public decimal Balance
		{
			get
			{
				return _Balance;
			}

			private set
			{
				if (_Balance != value)
				{
					_Balance = value;
					FirePropertyChanged("Balance");
					FirePropertyChanged("IsLive");
					FirePropertyChanged("CanTransfer");
					FireChange(new Change(ChangeTopic.Balance) { Amount = value });
				}
			}
		}

		public bool CanTransfer => Balance > 0;

		public virtual void UpdateBalance(decimal current)
		{
			Balance = current;
		}

		#endregion Balance

		#region Changes

		internal event Action<TokenStore, Change> Changed;

		public enum ChangeTopic
		{
			Balance,
			PendingTransfer,
		}

		internal void FireChange(Change change)
		{
			Changed?.Invoke(this, change);
		}

		public class Change
		{
			public Change(ChangeTopic topic)
			{
				Topic = topic;
			}

			public ChangeTopic Topic { get; private set; }

			public string OperationID { get; set; }
			public string ContraAccountID { get; set; }
			public decimal Amount { get; set; }
		}

		#endregion Changes

		#region Pending Operations

		public ObservableCollection<Change> PendingChanges { get; private set; } = new ObservableCollection<Change>();

		public bool HasPendingChanges => PendingChanges.Count > 0;

		public decimal AmountPending => PendingChanges.Sum(c => c.Amount);

		internal async void ExpectOperation(string operationID, string contraAccountID, decimal amount)
		{
			var change = new Change(ChangeTopic.PendingTransfer)
			{
				OperationID = operationID,
				ContraAccountID = contraAccountID,
				Amount = amount,
			};

			await AddPending(change);
		}

		internal async Task AddPending(Change change)
		{
			await PendingChanges.AddSynchronized(change);

			if (PendingChanges.Count == 1)
			{
				FirePropertyChanged("HasPendingChanges");
			}

			FirePropertyChanged("AmountPending");
		}

		internal async Task CloseOperation(string operationID, AccountEntry entry = null)
		{
			var cancelled = PendingChanges
				.FirstOrDefault(c => c.OperationID == operationID);

			if (cancelled != null)
			{
				await PendingChanges.RemoveSynchronized(cancelled);

				if (PendingChanges.Count == 0)
				{
					FirePropertyChanged("HasPendingChanges");
				}

				FirePropertyChanged("AmountPending");
			}

			if (entry != null)
			{
				Entries.Add(entry);

				EntryAdded?.Invoke(entry);
			}
		}

		#endregion Pending Operations

		#region Entries

		public event Action<AccountEntry> EntryAdded;

		public ObservableCollection<AccountEntry> Entries { get; private set; } = new ObservableCollection<AccountEntry>();

		public bool IsEntriesComplete { get; private set; }

		public async void CompleteEntries(Connection connection)
		{
			if (IsEntriesComplete)
			{
				return;
			}

			try
			{
				var entries = await connection.GetAccountEntries(AccountID);

				Entries = new ObservableCollection<AccountEntry>(entries);

				IsEntriesComplete = true;

				FirePropertyChanged("Entries");
				FirePropertyChanged("IsEntriesComplete");
			}
			catch (ServerException e)
			{
				Trace(e);
			}
		}

		#endregion Entries
	}
}