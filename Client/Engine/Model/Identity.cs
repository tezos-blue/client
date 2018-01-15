using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Model
{
	using Security;

	[Serializable]
	public partial class Identity : TokenStore, ISerializable
	{
		public Identity()
		{
			Stereotype = "Identity";
			accounts.Add(this);
		}

		public Identity(string passphrase) : this()
		{
			Keys = Guard.CreateKeyPair(passphrase);
		}

		// SECURITY
		public KeyPair Keys { get; protected set; }

		public override string AccountID => Keys.PublicKey.Hash;

		public override Identity Manager => this;

		public override bool IsLive => true;

		#region Balance

		public decimal TotalBalance => Accounts.Sum(account => account.Balance);

		public override void UpdateBalance(decimal change)
		{
			base.UpdateBalance(change);

			FirePropertyChanged("TotalBalance");
		}

		#endregion Balance

		#region Accounts

		private ObservableCollection<TokenStore> accounts = new ObservableCollection<TokenStore>();

		public IReadOnlyList<TokenStore> Accounts => accounts;

		internal async void ExpectOrigination(Account account, string operationID, string contraAccountID, decimal amount)
		{
			Trace($"Account originating | {account}");

			account.State = TokenStoreState.Creating;

			await AddAccount(account);

			await account.AddPending(new Change(ChangeTopic.PendingTransfer)
			{
				OperationID = operationID,
				Amount = amount,
				ContraAccountID = contraAccountID,
			});
		}

		public async Task AddAccount(Account account)
		{
			account.Changed += OnAccountChanged;
			account.SetManager(this);

			await accounts.AddSynchronized(account);
		}

		internal async Task DeleteAccount(Account account)
		{
			account.Changed -= OnAccountChanged;
			await accounts.RemoveSynchronized(account);
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
					FirePropertyChanged("TotalBalance");
					break;

				default:
					break;
			}
		}

		#endregion Accounts

		#region Credentials

		#region IsUnlocked

		private bool _IsUnlocked;

		public bool IsUnlocked
		{
			get
			{
				return _IsUnlocked;
			}

			private set
			{
				if (_IsUnlocked != value)
				{
					_IsUnlocked = value;
					FirePropertyChanged();
				}
			}
		}

		public bool Unlock(string passphrase)
		{
			IsUnlocked = true;
			return true;
		}

		#endregion IsUnlocked

		#endregion Credentials

		#region Serialization

		public Identity(SerializationInfo info, StreamingContext context) : this()
		{
			Name = info.GetString("Name");

			Keys = new KeyPair
			{
				PublicKey = (PublicKey)info.GetValue("PublicKey", typeof(PublicKey)),
				PrivateKey = (PrivateKey)info.GetValue("SecretKey", typeof(PrivateKey)),
			};
		}

		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Name", Name);
			info.AddValue("PublicKey", Keys.PublicKey, typeof(PublicKey));
			info.AddValue("SecretKey", Keys.PrivateKey, typeof(PrivateKey));
		}

		#endregion Serialization
	}
}