﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Model
{
	using Connections;
	using Cryptography;
	using Protocol;
	using Security;

	public sealed partial class Identity : TokenStore
	{
		public const string DefaultStereotype = "Identity";

		private IManageIdentities provider;

		public Identity()
		{
			Stereotype = DefaultStereotype;
			accounts.Add(this);
		}

		public Identity(PublicKey publicKey, IManageIdentities provider) : this()
		{
			PublicKey = publicKey;
			this.provider = provider;

			if (provider != null)
			{
				provider.RestoreIdentity(this);
			}
		}

		// SECURITY
		public PublicKey PublicKey { get; set; }

		public override string AccountID => PublicKey.Hash;

		public override Identity Manager => this;

		public override bool IsLive => true;

		protected override async Task<TokenStoreState> OnInitialize(Engine engine)
		{
			// Register and get info
			var register = new RegisterIdentityTask
			{
				IdentityID = AccountID,
				Name = Name,
				Stereotype = Stereotype,
				PublicKey = PublicKey.ToString(),
			};

			register = await engine.Connection.RegisterIdentity(register);

			var identityInfo = register.Info;

			// Initialize
			if (identityInfo != null && !identityInfo.IsUnknown)
			{
				Balance = identityInfo.Balance;

				// Managed Accounts
				var accounts = identityInfo.Accounts.Select(info =>
				{
					var account = new Account(info.Name, info.AccountID)
					{
						Stereotype = info.Stereotype,
						DelegateID = info.DelegateID,
					};

					account.Balance = info.Balance;
					account.UpdateState(info.State);

					return account;
				});

				foreach (var account in accounts)
				{
					engine.Cache(account);

					AddAccount(account);
				}
			}

			FirePropertyChanged(nameof(TotalBalance));

			return TokenStoreState.Online;
		}

		internal void Refresh(IdentityInfo info)
		{
			RefreshInfo(info);

			foreach (var accountInfo in info.Accounts)
			{
				var account = this[accountInfo.AccountID];

				if (account != null)
				{
					account.RefreshInfo(accountInfo);
				}
			}
		}

		#region Balance

		public decimal TotalBalance => Accounts.Sum(account => account.Balance);

		protected override void OnBalanceChanged()
		{
			FirePropertyChanged(nameof(TotalBalance));
		}

		#endregion Balance

		#region Accounts

		private ModelCollection<TokenStore> accounts = new ModelCollection<TokenStore>();

		public TokenStore[] Accounts => accounts.ToArray();

		public TokenStore this[string accountID]
			=> accounts.FirstOrDefault(account => account.AccountID == accountID);

		internal void AddAccount(Account account)
		{
			Trace($"Add {account}");

			account.Changed += OnAccountChanged;
			account.SetManager(this);

			accounts.Add(account);

			FirePropertyChanged(nameof(Accounts));
		}

		internal void ExpectOrigination(Account account, string operationID, string contraAccountID, decimal amount)
		{
			Trace($"Account originating | {account}");

			account.State = TokenStoreState.Creating;

			AddAccount(account);

			account.AddPending(new Change(ChangeTopic.PendingTransfer)
			{
				OperationID = operationID,
				Amount = amount,
				ContraAccountID = contraAccountID,
			});
		}

		internal async Task DeleteAccount(Account account, IConnection connection)
		{
			Trace($"Delete {account}");

			await connection.RemoveStaleAccount(account.AccountID, account.ManagerID);

			account.Changed -= OnAccountChanged;
			accounts.Remove(account);

			FirePropertyChanged(nameof(Accounts));
		}

		internal IEnumerable<TokenStore> GetAvailableTransferSources()
		{
			var list = new List<TokenStore>();

			list.AddRange(Accounts.Where(a => a.CanTransfer));

			return list;
		}

		private void OnAccountChanged(TokenStore account, Account.Change change)
		{
			switch (change.Topic)
			{
				case Account.ChangeTopic.Balance:
					FirePropertyChanged(nameof(TotalBalance));
					break;

				default:
					break;
			}
		}

		#endregion Accounts

		#region Lock

		public bool IsUnlocked
		{
			get
			{
				if (provider != null)
				{
					return provider.IsUnlocked(AccountID);
				}

				return false;
			}
		}

		public bool IsLocked => !IsUnlocked;

		public bool Unlock(Passphrase passphrase)
		{
			Trace($"Unlock");

			if (provider == null) throw new InvalidOperationException("No identity provider configured.");

			var result = provider.Unlock(AccountID, passphrase);

			FirePropertyChanged(nameof(IsLocked));
			FirePropertyChanged(nameof(IsUnlocked));

			return result;
		}

		public void Lock()
		{
			Trace($"Lock");

			if (provider == null) throw new InvalidOperationException("No identity provider configured.");

			provider.Lock(AccountID);
		}

		#endregion Lock

		#region Backup

		public byte[] BackupData
			=> provider.GetBackupData(AccountID);
		#endregion Backup
	}
}