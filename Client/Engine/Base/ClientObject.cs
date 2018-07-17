using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	public class ClientObject : TezosObject, INotifyPropertyChanged
	{
		private static TaskScheduler scheduler;

		public static async Task ExecuteSynchronized(Action action)
		{
			if (scheduler == TaskScheduler.Current)
			{
				action();
			}
			else
			{
				await Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, scheduler ?? TaskScheduler.Current);
			}
		}

		internal static void SetSynchronizationContext()
		{
			scheduler = TaskScheduler.FromCurrentSynchronizationContext();
		}

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		public async void FirePropertyChanged([CallerMemberName]string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				if (scheduler != null)
				{
					await ExecuteSynchronized(() =>
					{
						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
					});
				}
				else
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
				}
			}
		}

		#endregion INotifyPropertyChanged
	}
}