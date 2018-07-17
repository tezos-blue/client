using System;
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
		UnheardOf,
		Changing,
		Retired,
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

			internal set
			{
				if (_Name != value)
				{
					_Name = value;
					FirePropertyChanged(nameof(Name));
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
					FirePropertyChanged(nameof(IsChanging));
				}
			}
		}

		public bool IsChanging => State == TokenStoreState.Changing || State == TokenStoreState.Creating;

		public virtual void UpdateState(AccountState accountState)
		{
			switch (accountState)
			{
				case AccountState.NotFound:
					State = TokenStoreState.UnheardOf;
					break;

				case AccountState.Archived:
					State = TokenStoreState.Retired;
					break;

				case AccountState.Cached:
					State = TokenStoreState.Offline;
					break;

				case AccountState.Live:
					State = TokenStoreState.Online;
					break;
			}
		}

		#endregion State

		#region Manager

		protected string storedManagerID;

		public abstract Identity Manager { get; }

		public string ManagerID => Manager == null ? storedManagerID : Manager.AccountID;

		#endregion Manager

		public abstract bool IsLive { get; }

		public virtual bool IsDelegated => false;

		public bool IsIdentityAccount
			=> AccountID == ManagerID;

		public override string ToString()
		{
			return $"{AccountID.Substring(0, Math.Min(10, AccountID.Length))}...";
		}

		#region Initialization

		public SyncEvent WhenInitialized = new SyncEvent();
		private bool isInitializing;

		internal async Task Initialize(Engine engine)
		{
			Trace($"Initialize");

			if (BeginInitialize())
			{
				try
				{
					State = await OnInitialize(engine);
				}
				catch
				{
					State = TokenStoreState.Offline;
				}

				EndInitialize();
			}
		}

		internal async Task Refresh(Engine engine)
		{
			Trace($"Refresh");

			if (BeginInitialize())
			{
				try
				{
					State = await RefreshInfo(engine);
				}
				catch
				{
					State = TokenStoreState.Offline;
				}

				EndInitialize();
			}
		}

		protected abstract Task<TokenStoreState> OnInitialize(Engine engine);

		private bool BeginInitialize()
		{
			if (!isInitializing)
			{
				isInitializing = true;

				// Start new
				State = TokenStoreState.Initializing;
				WhenInitialized = new SyncEvent();

				return true;
			}
			else
			{
				// Already one running
				return false;
			}
		}

		private void EndInitialize()
		{
			isInitializing = false;

			WhenInitialized.SetComplete();
		}

		#endregion Initialization

		protected async Task<TokenStoreState> RefreshInfo(Engine engine)
		{
			Trace($"Refresh AccountInfo");

			try
			{
				var info = await engine.Connection.GetAccountInfo(AccountID);

				Balance = info.Balance;

				return TokenStoreState.Online;
			}
			catch (ServerException e)
			{
				Trace(e);

				switch (e.ServerError)
				{
					case ServerError.AccountNotFound:
						return TokenStoreState.UnheardOf;

					default:
						return TokenStoreState.Offline;
				}
			}
			catch (Exception e)
			{
				Trace(e);
				return TokenStoreState.Offline;
			}
		}

		#region Balance

		private decimal _Balance;

		public decimal Balance
		{
			get
			{
				return _Balance;
			}

			internal set
			{
				if (_Balance != value)
				{
					_Balance = value;
					FirePropertyChanged(nameof(Balance));
					FirePropertyChanged(nameof(IsLive));
					FirePropertyChanged(nameof(CanTransfer));
					FireChange(new Change(ChangeTopic.Balance) { Amount = value });
					OnBalanceChanged();
				}
			}
		}

		public bool CanTransfer => Balance > 0;

		protected virtual void OnBalanceChanged()
		{
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

			public override string ToString()
				=> $"Change: {Topic} | {OperationID}";
		}

		#endregion Changes

		#region Pending Operations

		private ModelCollection<Change> pendingChanges = new ModelCollection<Change>();

		public Change[] PendingChanges => pendingChanges.ToArray();

		public bool HasPendingChanges => pendingChanges.Count > 0;

		public decimal AmountPending => pendingChanges.Sum(c => c.Amount);

		internal void ExpectOperation(string operationID, string contraAccountID, decimal amount)
		{
			var change = new Change(ChangeTopic.PendingTransfer)
			{
				OperationID = operationID,
				ContraAccountID = contraAccountID,
				Amount = amount,
			};

			AddPending(change);
		}

		internal void AddPending(Change change)
		{
			Trace($"Add pending: {change}");

			pendingChanges.Add(change);

			if (pendingChanges.Count == 1)
			{
				FirePropertyChanged(nameof(HasPendingChanges));
			}

			FirePropertyChanged(nameof(PendingChanges));
			FirePropertyChanged(nameof(AmountPending));
		}

		internal void CloseOperation(string operationID, AccountEntry entry = null)
		{
			Trace($"Close pending: {operationID}");

			var cancelled = pendingChanges
				.FirstOrDefault(c => c.OperationID == operationID);

			if (cancelled != null)
			{
				pendingChanges.Remove(cancelled);

				if (pendingChanges.Count == 0)
				{
					FirePropertyChanged(nameof(HasPendingChanges));
				}

				FirePropertyChanged(nameof(AmountPending));
				FirePropertyChanged(nameof(PendingChanges));
			}

			AddEntry(entry);
		}

		#endregion Pending Operations

		#region Entries

		private ModelCollection<AccountEntry> entries = new ModelCollection<AccountEntry>();

		public event Action<AccountEntry> EntryAdded;

		public AccountEntry[] Entries => entries.ToArray();

		public bool IsEntriesComplete { get; private set; }

		public async void CompleteEntries(IConnection connection)
		{
			if (IsEntriesComplete)
			{
				return;
			}

			try
			{
				Trace($"Complete entries");

				var entries = await connection.GetAccountEntries(AccountID);

				this.entries = new ModelCollection<AccountEntry>(entries);

				IsEntriesComplete = true;

				FirePropertyChanged(nameof(Entries));
				FirePropertyChanged(nameof(IsEntriesComplete));
			}
			catch (ServerException e)
			{
				Trace(e);
			}
		}

		internal void AddEntry(AccountEntry entry)
		{
			if (entry != null)
			{
				entries.Add(entry);

				if (EntryAdded != null)
				{
					var task = ExecuteSynchronized(() => EntryAdded(entry));
				}
			}

			FirePropertyChanged(nameof(Entries));
		}

		#endregion Entries
	}
}