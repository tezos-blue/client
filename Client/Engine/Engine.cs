﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Connections;
	using Cryptography;
	using Model;
	using OS;
	using Protocol;
	using Security;

	public class EngineConfiguration
	{
		public IConnection Connection { get; set; }

		public IStoreLocal LocalStorage { get; set; }

		public TargetPlatform Platform { get; set; }

		public string InstanceID { get; set; }

		public string ApplicationVersion { get; set; }

		public bool InTestMode { get; set; }
	}

	public partial class Engine : ClientObject
	{
		#region TokenStore cache

		private Dictionary<string, TokenStore> accounts = new Dictionary<string, TokenStore>();

		public TokenStore this[string accountID]
		{
			get
			{
				accounts.TryGetValue(accountID, out TokenStore found);

				return found;
			}
		}

		internal void Cache(TokenStore tokenStore)
		{
			Trace($"Cache {tokenStore}");

			accounts[tokenStore.AccountID] = tokenStore;
		}

		private void Uncache(TokenStore tokenStore)
		{
			Trace($"Uncache {tokenStore}");

			accounts.Remove(tokenStore.AccountID);
		}

		#endregion TokenStore cache

		public IConnection Connection => configuration.Connection;

		internal Task<decimal> GetBalance(string accountID)
		{
			Trace($"Query Balance for {accountID}");

			return Connection.GetBalance(accountID);
		}

		#region Initialization

		private EngineConfiguration configuration;

		public Engine(EngineConfiguration configuration)
		{
			this.configuration = configuration;

			Connection.EventReceived += OnNetworkEvent;

			if (configuration.LocalStorage != null)
			{
				signProviders.Add(new SoftwareVault(configuration.LocalStorage));
			}

			Trace($"Created");
		}

		public static void SynchronizeContext(SynchronizationContext current)
		{
			ClientObject.SetSynchronizationContext();
		}

		public void AddProviderIdentities(IProvideSigning provider)
		{
			foreach (var identityID in provider.IdentityIDs)
			{
				Trace($"Add provider identity: {identityID}");

				identities.Add(new Identity(
					new PublicKey(provider.GetPublicKey(identityID)),
					provider as IManageIdentities));
			}

			FirePropertyChanged(nameof(Identities));
		}

		private async Task InitializeProviders()
		{
			try
			{
				Trace($"Initialize");

				// Wait for all providers to be initialized
				await Task.WhenAll(signProviders.Select(async p => await p.Initialize()));

				// Get identities from providers
				foreach (var provider in signProviders)
				{
					AddProviderIdentities(provider);
				}

				FirePropertyChanged("Identities");

				if (identities.Any())
				{
					FirePropertyChanged("DefaultIdentity");
				}
			}
			catch (Exception e)
			{
				Trace(e);
				throw;
			}
		}

		#endregion Initialization

		#region Signing Providers

		private List<IProvideSigning> signProviders = new List<IProvideSigning>();

		public IManageIdentities DefaultIdentityProvider => signProviders.OfType<IManageIdentities>().FirstOrDefault();

		public async Task Register(IProvideSigning provider, int priority = 0)
		{
			if (provider == null) throw new ArgumentNullException(nameof(provider));

			Trace($"Register with priority {priority}: {provider}");

			// Initialize provider
			await provider.Initialize();

			// Insert provider according to priority
			priority = Math.Min(priority, signProviders.Count);
			signProviders.Insert(priority, provider);

			FirePropertyChanged(nameof(DefaultIdentityProvider));

			// Add identities on hot plugin
			if (IsIdentitiesInitialized)
			{
				AddProviderIdentities(provider);
			}
		}

		#endregion Signing Providers

		#region Identities

		private ModelCollection<Identity> identities = new ModelCollection<Identity>();

		public Identity DefaultIdentity => Identities?.FirstOrDefault();

		public IEnumerable<Identity> Identities => identities.ToArray();

		public async Task<Identity> AddIdentity(string stereotype, string identityName, Passphrase passphrase, bool unlock = false)
		{
			Trace($"Add Identity {identityName}");

			// Get identity provider
			var provider = DefaultIdentityProvider;

			if (provider == null)
			{
				throw new ApplicationException("No identity provider configured");
			}

			var identity = await provider.CreateIdentity(identityName, passphrase, stereotype);

			var initTask = AddIdentity(identity);

			if (unlock)
			{
				identity.Unlock(passphrase);
			}

			return identity;
		}

		public async Task<Identity> ImportIdentity(string stereotype, string identityName, KeyPair keyPair)
		{
			Trace($"Import Identity {identityName}");

			// Get identity provider
			var provider = DefaultIdentityProvider;

			if (provider == null)
			{
				throw new ApplicationException("No identity provider configured");
			}

			var imported = await provider.ImportIdentity(identityName, keyPair, stereotype);

			// Already known?
			var found = identities.FirstOrDefault(i => i.AccountID == imported.AccountID);

			if (found == null)
			{
				// New Identity
				var initTask = AddIdentity(imported);
				return imported;
			}
			else
			{
				return found;
			}
		}

		public Task PurgeAll()
		{
			accounts.Clear();
			identities.Clear();

			FirePropertyChanged(nameof(DefaultIdentity));
			FirePropertyChanged(nameof(Identities));

			return configuration?.LocalStorage?.PurgeAll();
		}

		public Identity FindIdentity(string identityID)
			=> identities.FirstOrDefault(i => i.AccountID == identityID);

		// Internal add
		private async Task AddIdentity(Identity identity)
		{
			// Add to known
			Cache(identity);

			identities.Add(identity);

			if (identities.Count == 1)
			{
				FirePropertyChanged(nameof(DefaultIdentity));
			}

			FirePropertyChanged(nameof(Identities));

			// Initialize
			try
			{
				await identity.Initialize(this);
			}
			catch (Exception e)
			{
				Trace(e);
			}
		}

		private async Task InitializeIdentities()
		{
			Trace($"Initialize Identities");

			// Initialize each identity
			await Task.WhenAll(identities.Select(async i =>
			{
				Cache(i);
				await i.Initialize(this);
			}));

			IsIdentitiesInitialized = true;

			Trace("Identities initialized");
		}

		private void LockAll()
		{
			if (identities != null)
			{
				Trace($"Lock all identities");

				foreach (var identity in identities)
				{
					identity.Lock();
				}
			}
		}

		#region IsIdentitiesInitialized

		private bool _IsIdentitiesInitialized;

		public bool IsIdentitiesInitialized
		{
			get
			{
				return _IsIdentitiesInitialized;
			}

			set
			{
				if (_IsIdentitiesInitialized != value)
				{
					_IsIdentitiesInitialized = value;
					FirePropertyChanged();
				}
			}
		}

		#endregion IsIdentitiesInitialized

		#endregion Identities

		#region Accounts

		public IEnumerable<TokenStore> GetAvailableTransferSources()
		{
			return Identities.SelectMany(i => i.GetAvailableTransferSources());
		}

		public IEnumerable<TokenStore> GetAvailableDestinations()
		{
			return Identities.SelectMany(i => i.Accounts);
		}

		public bool IsValidAccountID(string accountID)
		{
			if (string.IsNullOrWhiteSpace(accountID)) return false;

			try
			{
				CryptoServices.DecodePrefixed(HashType.PublicKeyHash, accountID);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async void DeleteAccount(Account account)
		{
			Trace($"Delete {account}");

			var manager = Identities.FirstOrDefault(i => i.AccountID == account.ManagerID);

			if (manager != null)
			{
				await manager.DeleteAccount(account, Connection);
			}

			Uncache(account);
		}

		private async Task RefreshAccounts()
		{
			Trace($"Refresh accounts");

			var accounts = Identities.SelectMany(i => i.Accounts.Where(a => a.IsLive));

			var tasks = accounts.Select(a => a.Refresh(this));

			await Task.WhenAll(tasks);
		}

		private async Task RefreshIdentities()
		{
			Trace($"Refresh identities");

			async Task RefreshIdentity(Identity identity)
			{
				var info = await Connection.GetIdentityInfo(identity.AccountID);

				identity.Refresh(info);
			}

			var tasks = Identities.Select(i => RefreshIdentity(i));

			await Task.WhenAll(tasks);
		}



		#endregion Accounts

		#region ConnectionState

		private ConnectionState _ConnectionState = ConnectionState.Disconnected;

		public event Action<ConnectionState> ConnectionStateChanged;

		public ConnectionState ConnectionState
		{
			get { return _ConnectionState; }
			set
			{
				if (_ConnectionState != value)
				{
					_ConnectionState = value;

					Trace($"ConnectionState: {value}");

					FirePropertyChanged();
					FirePropertyChanged("IsConnected");

					ConnectionStateChanged?.Invoke(value);
				}
			}
		}

		public bool IsConnected
			=> ConnectionState >= ConnectionState.Connected;

		#endregion ConnectionState

		#region Life Cycle

		public readonly SyncEvent WhenInitialized = new SyncEvent();

		public event Action<bool> Initialized;

		public async Task Start()
		{
			Trace($"Start");

			// Parallel: Load Identities and connect to Service
			await Task.WhenAll(
				InitializeProviders(),
				ConnectToService());

			// Connected now, get info about identities
			Task initialization = InitializeIdentities();
			Task timeout = Task.Delay(IdentitiesInitializedTimeout);

			var first = await Task.WhenAny(
				initialization,
				timeout
				);

			// Notify
			Trace($"Initialized");
			Initialized?.Invoke(first == initialization);
			WhenInitialized.SetComplete();
		}

		public async Task Resume()
		{
			Trace($"Resume");

			LockAll();

			//await RefreshAccounts();
			await RefreshIdentities();
		}

		public void Suspend()
		{
			Trace($"Suspend");

			LockAll();
		}

		private async Task ConnectToService()
		{
			Trace("Start Connection Process");

			var registration = new InstanceInfo
			{
				InstanceID = configuration.InstanceID,
				Platform = configuration.Platform,
				ApplicationVersion = configuration.ApplicationVersion,
			};

			// Repeat until connected
			while (!IsConnected)
			{
				ConnectionState = ConnectionState.Connecting;

				try
				{
					Trace("Try to connect...");

					ConnectionState = await Connection.Connect(registration);
				}
				catch (Exception e)
				{
					Trace($"... failed: {e.Message}");

					ConnectionState = ConnectionState.Disconnected;

					await Task.Delay(TimeBetweenConnectionAttempts);
				}
			}

			Trace("Connected");
		}

		#endregion Life Cycle
	}
}