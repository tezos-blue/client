﻿using System;
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

		internal void InjectNetworkEvent(NetworkEvent networkEvent)
			=> OnNetworkEvent(networkEvent);

		private void OnNetworkEvent(NetworkEvent networkEvent)
		{
			switch (networkEvent)
			{
				case OriginatePendingEvent originate:
					{
						var identity = Identities
							.FirstOrDefault(i => i.AccountID == originate.ManagerID);

						if (identity != null)
						{
							var found = identity.Accounts
								.Where(a => a.AccountID == originate.AccountID)
								.FirstOrDefault();

							if (found == null)
							{
								var account = new Account(originate.Name, originate.AccountID)
								{
									Stereotype = originate.Stereotype,
									DelegateID = originate.DelegateID,
								};

								identity.ExpectOrigination(
									account,
									originate.OperationID,
									originate.ContraAccountID,
									originate.Amount);

								OperationTaskflow.Update(originate.OperationID, TaskProgress.Acknowledged); 
							}
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
								account.CloseOperation(originate.OperationID, originate.Entry);
							}
							else
							{
								// switched on later
								account = new Account(originate.Name, originate.AccountID)
								{
									Stereotype = originate.Stereotype,
									DelegateID = originate.DelegateID,
								};

								identity.AddAccount(account);

								account.AddEntry(originate.Entry);
							}

							account.Balance = originate.Balance;

							account.State = TokenStoreState.Online;

							Cache(account);

							OperationTaskflow.Update(originate.OperationID, TaskProgress.Confirmed);
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
								account.CloseOperation(opTimeout.OperationID);
								account.State = TokenStoreState.UnheardOf;
							}

							OperationTaskflow.Update(opTimeout.OperationID, TaskProgress.Timeout);
						}
					}
					break;

				case BalanceChangedEvent changeBalance:
					{
						if (IsTooOld(changeBalance.BlockIndex))
							return;

						if (accounts.TryGetValue(changeBalance.AccountID, out TokenStore account))
						{
							account.Balance = changeBalance.Balance;
							account.UpdateState(changeBalance.State);
							account.CloseOperation(changeBalance.OperationID, changeBalance.Entry);
						}

						OperationTaskflow.Update(changeBalance.OperationID, TaskProgress.Confirmed);
					}
					break;

				case TransactionPendingEvent transactionPending:
					{
						if (accounts.TryGetValue(transactionPending.AccountID, out TokenStore account))
						{
							if (account.ExpectOperation(
								transactionPending.OperationID,
								transactionPending.ContraAccountID,
								transactionPending.Amount))
							{
								account.State = TokenStoreState.Changing;

								OperationTaskflow.Update(transactionPending.OperationID, TaskProgress.Acknowledged);
							}
						}
					}
					break;

				case TransactionTimeoutEvent opTimeout:
					{
						Debug.Assert(opTimeout.AccountID != null);

						if (accounts.TryGetValue(opTimeout.AccountID, out TokenStore account))
						{
							account.CloseOperation(opTimeout.OperationID);

							OperationTaskflow.Update(opTimeout.OperationID, TaskProgress.Timeout);
						}
					}
					break;

				case ActivationPendingEvent activationPending:
					{
						if (accounts.TryGetValue(activationPending.IdentityID, out TokenStore account))
						{
							if (account.ExpectOperation(
								activationPending.OperationID,
								null,
								activationPending.Amount))
							{
								account.State = TokenStoreState.Changing;

								OperationTaskflow.Update(activationPending.OperationID, TaskProgress.Acknowledged);
							}
						}
					}
					break;

				case ActivationTimeoutEvent opTimeout:
					{
						Debug.Assert(opTimeout.IdentityID != null);

						if (accounts.TryGetValue(opTimeout.IdentityID, out TokenStore account))
						{
							account.CloseOperation(opTimeout.OperationID);

							OperationTaskflow.Update(opTimeout.OperationID, TaskProgress.Timeout);
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