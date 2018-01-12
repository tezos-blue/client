using System;
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
	using Security;
	using Protocol;

	public class WalletEngineConfiguration
	{
		public Connection Connection { get; set; }

		public IStoreLocal OSLocalStorage { get; set; }

		public TargetPlatform Platform { get; set; }
	}

	public partial class WalletEngine : ClientObject
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

		public Connection Connection => configuration.Connection;

		internal async Task<decimal> GetBalance(string accountID)
		{
			return await Connection.GetBalance(accountID);
		}

		#region Initialization

		private WalletEngineConfiguration configuration;

		private Task initializationTask;

		public WalletEngine(WalletEngineConfiguration configuration)
		{
			this.configuration = configuration;

			Connection.EventReceived += OnNetworkEvent;

			initializationTask = Task.Run(InitializeEngine);
		}

		public static void SynchronizeContext(SynchronizationContext current)
		{
			ClientObject.SetSynchronizationContext();
		}

		private async Task InitializeEngine()
		{
			try
			{
				await InitializeStorage();
				await InitializeIdentities();

				Trace("Identities initialized");

				await InitializeAccounts();

				Trace("Accounts initialized");

				foreach (var identity in Identities)
				{
					identity.FirePropertyChanged("TotalBalance");
				}
			}
			catch (Exception e)
			{
				Trace(e);
				throw;
			}
		}

		#endregion Initialization

		#region Identities

		private ObservableCollection<Identity> myIdentities;

		public Identity DefaultIdentity => Identities?.FirstOrDefault();

		public IEnumerable<Identity> Identities => myIdentities;

		public async Task AddIdentity(string identityName, string identityPassphrase)
		{
			var identity = new Identity
			{
				Name = identityName,
			};

			await store.StoreIdentity(identity);

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

			//await myIdentities.AddSynchronized(identity);
			myIdentities.Add(identity);

			if (myIdentities.Count == 1)
			{
				FirePropertyChanged("DefaultIdentity");
			}
		}

		private async Task InitializeIdentities()
		{
			myIdentities = new ObservableCollection<Identity>(await store.LoadIdentities());

			FirePropertyChanged("Identities");

			if (myIdentities.Any())
			{
				// Check Balance of identities
				foreach (var identity in myIdentities)
				{
					Cache(identity);

					await identity.Initialize(Connection);
				}

				FirePropertyChanged("DefaultIdentity");
			}

			IsIdentitiesInitialized = true;
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
			return true;
		}

		public async void DeleteAccount(Account account)
		{
			try
			{
				var manager = Identities.FirstOrDefault(i => i.AccountID == account.ManagerID);

				if (manager != null)
				{
					await manager.DeleteAccount(account);
				}
				else
				{
					//watchedAccounts.Remove(account);
				}

				Uncache(account);
			}
			finally
			{
				await store.DeleteAccount(account);
			}
		}

		private async Task InitializeAccounts()
		{
			var accounts = await store.LoadAccounts();

			var tasks = accounts.Select(a => InitializeAccount(a));

			await Task.WhenAll(tasks);
		}

		private async Task InitializeAccount(Account account)
		{
			try
			{
				var manager = Identities.FirstOrDefault(i => i.AccountID == account.ManagerID);

				if (manager != null)
				{
					await manager.AddAccount(account);
					account.SetManager(manager);
				}
				else
				{
					//watchedAccounts.AddSynchronized(account);
				}

				Cache(account);

				await account.Initialize(Connection);
			}
			catch (ServerException e)
			{
				Trace(e);

				return;
			}
			catch (Exception e)
			{
				Trace(e);

				await store.DeleteAccount(account);
				return;
			}
		}

		private async Task RefreshAccounts()
		{
			var accounts = Identities.SelectMany(i => i.Accounts.Where(a => a.IsLive));

			var tasks = accounts.Select(async a => await a.Initialize(Connection));

			await Task.WhenAll(tasks);
		}

		#endregion Accounts

		#region Storage

		private LocalStore store;

		private async Task InitializeStorage()
		{
			store = await LocalStore.Open(configuration.OSLocalStorage);
		}

		#endregion Storage

		//#region Network

		//public IEnumerable<Node> Peers { get; private set; }
		//public IEnumerable<Node> Nodes { get; private set; }

		//public async Task RefreshNodes()
		//{
		//	var networkInfo = await Connection.GetNetworkInfo();

		//	Nodes = networkInfo.OurNodes.Select(sn => new Node(sn));
		//	Peers = networkInfo.PeerNodes.Select(sn => new Node(sn));

		//	FirePropertyChanged("Nodes");
		//	FirePropertyChanged("Peers");
		//}

		//#endregion Network

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
			await Task.Run(DoConnect);
		}

		public async Task Resume()
		{
			await RefreshAccounts();
			//await Task.Run(DoConnect);
		}

		public void Suspend()
		{
			//Connection.Disconnect();

			//ConnectionState = ConnectionState.Diconnected;
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
				InstanceID = store.InstanceID,
				Platform = configuration.Platform,
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