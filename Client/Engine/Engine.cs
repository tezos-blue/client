﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	using Connections;
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

		private void Cache(TokenStore tokenStore)
		{
			accounts.Add(tokenStore.AccountID, tokenStore);
		}

		private void Uncache(TokenStore tokenStore)
		{
			accounts.Remove(tokenStore.AccountID);
		}

		#endregion TokenStore cache

		public IConnection Connection => configuration.Connection;

		internal async Task<decimal> GetBalance(string accountID)
		{
			return await Connection.GetBalance(accountID);
		}

		#region Initialization

		private EngineConfiguration configuration;

		private Task initializationTask;

		public Engine(EngineConfiguration configuration)
		{
			this.configuration = configuration;

			Connection.EventReceived += OnNetworkEvent;

			if (configuration.LocalStorage != null)
			{
				signProviders.Add(new SoftwareVault(configuration.LocalStorage));
			}

			initializationTask = Task.Run(InitializeEngine);
		}

		public static void SynchronizeContext(SynchronizationContext current)
		{
			ClientObject.SetSynchronizationContext();
		}

		public void AddProviderIdentities(IProvideSigning provider)
		{
			foreach (var identityID in provider.IdentityIDs)
			{
				identities.Add(new Identity(
					new PublicKey(provider.GetPublicKey(identityID)),
					provider as IManageIdentities));
			}
		}

		private async Task InitializeEngine()
		{
			try
			{
				// Wait for all providers to be initialized
				await Task.WhenAll(signProviders.Select(async p => await p.Initialize()));

				// Get identities from providers
				identities = new ObservableCollection<Identity>();

				foreach (var provider in signProviders)
				{
					AddProviderIdentities(provider);
				}

				FirePropertyChanged("Identities");

				if (identities.Any())
				{
					FirePropertyChanged("DefaultIdentity");
				}

				// Initialize each identity
				await Task.WhenAll(identities.Select(async i =>
				{
					Cache(i);
					await i.Initialize(this);
				}));

				IsIdentitiesInitialized = true;
				Trace("Identities initialized");
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

		private ObservableCollection<Identity> identities;

		public Identity DefaultIdentity => Identities?.FirstOrDefault();

		public IEnumerable<Identity> Identities => identities;

		public async Task<Identity> AddIdentity(string identityName, string passphrase, bool unlock = false)
		{
			// Get identity provider
			var provider = DefaultIdentityProvider;

			if (provider == null)
			{
				throw new ApplicationException("No identity provider configured");
			}

			var identity = await provider.CreateIdentity(identityName, passphrase);

			Cache(identity);

			try
			{
				await Connection.Monitor(new[] { identity.AccountID });
			}
			catch (Exception e)
			{
				Trace(e);
			}

			identity.State = TokenStoreState.Online;

			identities.Add(identity);

			if (identities.Count == 1)
			{
				FirePropertyChanged("DefaultIdentity");
			}

			if (unlock)
			{
				identity.Unlock(passphrase);
			}

			return identity;
		}

		private void LockAll()
		{
			foreach (var identity in identities)
			{
				identity.Lock();
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

		// SECURITY
		// TODO validate public key hash
		public bool IsValidAccountID(string accountID)
		{
			return true;
		}

		public async void DeleteAccount(Account account)
		{
				var manager = Identities.FirstOrDefault(i => i.AccountID == account.ManagerID);

				if (manager != null)
				{
					await manager.DeleteAccount(account, Connection);
				}

				Uncache(account);
		}

		private async Task RefreshAccounts()
		{
			var accounts = Identities.SelectMany(i => i.Accounts.Where(a => a.IsLive));

			var tasks = accounts.Select(async a => await a.Initialize(this));

			await Task.WhenAll(tasks);
		}

		#endregion Accounts

		#region ConnectionState

		private ConnectionState _ConnectionState;

		public event Action<ConnectionState> ConnectionStateChanged;

		public ConnectionState ConnectionState
		{
			get { return _ConnectionState; }
			set
			{
				if (_ConnectionState != value)
				{
					_ConnectionState = value;

					FirePropertyChanged();
					FirePropertyChanged("IsConnected");

					ConnectionStateChanged?.Invoke(value);
				}
			}
		}

		public bool IsConnected => ConnectionState == ConnectionState.Connected;

		#endregion ConnectionState

		#region Life Cycle

		public async Task Start()
		{
			await DoConnect();
		}

		public async Task Resume()
		{
			LockAll();

			await RefreshAccounts();
		}

		public void Suspend()
		{
			LockAll();
		}

		private async Task DoConnect()
		{
			await initializationTask;

			Trace("Start Connection Process");

			var accountIDs = Identities
				.SelectMany(i => i.Accounts)
				.Select(a => a.AccountID);

			var registration = new InstanceInfo
			{
				MonitoredAccounts = accountIDs.ToArray(),
				InstanceID = configuration.InstanceID,
				Platform = configuration.Platform,
				ApplicationVersion = configuration.ApplicationVersion,
			};

			while (ConnectionState != ConnectionState.Connected)
			{
				ConnectionState = ConnectionState.Connecting;

				try
				{
					Trace("Try to connect...");

					await Connection.Connect(registration);

					ConnectionState = ConnectionState.Connected;
				}
				catch
				{
					Trace("... failed");

					ConnectionState = ConnectionState.Disconnected;

					await Task.Delay(TimeBetweenConnectionAttempts);
				}
			}

			Trace("Connected");
		}

		#endregion Life Cycle
	}
}