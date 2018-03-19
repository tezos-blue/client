using System;
using System.Diagnostics;
using System.Linq;

namespace SLD.Tezos.Client
{
	using Flow;
	using Model;
	using Protocol;

	partial class Engine
	{
		public event Action<Engine, ServiceEvent> ServiceEventReceived;

		private async void OnNetworkEvent(NetworkEvent networkEvent)
		{
			switch (networkEvent)
			{
				case OriginatePendingEvent originate:
					{
						var identity = Identities
							.FirstOrDefault(i => i.AccountID == originate.ManagerID);

						if (identity != null)
						{
							var account = new Account(originate.Name, originate.AccountID);

							identity.ExpectOrigination(
								account,
								originate.OperationID,
								originate.ContraAccountID,
								originate.Amount);

							ProtectedTaskflow.Update(originate.OperationID, TaskProgress.Acknowledged);
						}
					}
					break;

				case OriginateEvent originate:
					{
						if (IsTooOld(originate.BlockIndex))
							return;

						if (accounts.ContainsKey(originate.AccountID))
							return;

						var identity = Identities
							.FirstOrDefault(i => i.AccountID == originate.ManagerID);

						if (identity != null)
						{
							if (identity.Accounts
								.FirstOrDefault(a => a.AccountID == originate.AccountID) is Account account)
							{
								// OriginatePendingEvent received before
								await account.CloseOperation(originate.OperationID, originate.Entry);
							}
							else
							{
								// switched on later
								account = new Account(originate.Name, originate.AccountID);

								await identity.AddAccount(account);

								account.Entries.Insert(0, originate.Entry);
							}

							account.UpdateBalance(originate.Balance);

							account.State = TokenStoreState.Online;

							Cache(account);

							ProtectedTaskflow.Update(originate.OperationID, TaskProgress.Confirmed);
						}
					}
					break;

				case TransactionPendingEvent transactionPending:
					{
						if (accounts.TryGetValue(transactionPending.AccountID, out TokenStore account))
						{
							account.ExpectOperation(
								transactionPending.OperationID,
								transactionPending.ContraAccountID,
								transactionPending.Amount);

							account.State = TokenStoreState.Changing;
						}
					}
					break;

				case BalanceChangedEvent changeBalance:
					{
						if (IsTooOld(changeBalance.BlockIndex))
							return;

						if (accounts.TryGetValue(changeBalance.AccountID, out TokenStore account))
						{
							account.UpdateBalance(changeBalance.Balance);
							account.UpdateState(changeBalance.State);
							await account.CloseOperation(changeBalance.OperationID, changeBalance.Entry);
						}
					}
					break;

				case OriginationTimeoutEvent opTimeout:
					{
						var identity = Identities
							.FirstOrDefault(i => i.AccountID == opTimeout.ManagerID);

						if (identity != null)
						{
							if (identity.Accounts
								.FirstOrDefault(a => a.AccountID == opTimeout.AccountID) is Account account)
							{
								await account.CloseOperation(opTimeout.OperationID);
								account.State = TokenStoreState.UnheardOf;
							}
						}
					}
					break;

				case TransactionTimeoutEvent opTimeout:
					{
						Debug.Assert(opTimeout.AccountID != null);

						if (accounts.TryGetValue(opTimeout.AccountID, out TokenStore account))
						{
							await account.CloseOperation(opTimeout.OperationID);
						}
					}
					break;

				case ServiceEvent svc:
					{
						OnServiceEvent(svc);

						ServiceEventReceived?.Invoke(this, svc);
					}
					break;

				default:
					break;
			}
		}

		#region Filter old messages

		private int CurrentBlockIndex = 0;

		private bool IsTooOld(int blockIndex)
		{
			if (blockIndex > CurrentBlockIndex)
			{
				CurrentBlockIndex = blockIndex;
				return false;
			}

			return blockIndex < CurrentBlockIndex;
		}

		#endregion Filter old messages

		#region Service messages

		private ServiceState _ServiceState = ServiceState.Operational;

		public event Action<Engine> ServiceStateChanged;

		public ServiceState ServiceState
		{
			get => _ServiceState;

			private set
			{
				if (_ServiceState != value)
				{
					_ServiceState = value;
					FirePropertyChanged();

					ServiceStateChanged?.Invoke(this);
				}
			}
		}

		private void OnServiceEvent(ServiceEvent serviceEvent)
		{
			ServiceState = serviceEvent.ServiceState;
		}

		#endregion Service messages
	}
}