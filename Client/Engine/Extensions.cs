using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SLD.Tezos.Client
{
	public static class ClientExtensions
	{
		public static async Task AddSynchronized<T>(this ObservableCollection<T> collection, T item)
		{
			await ClientObject.ExecuteSynchronized(() => collection.Add(item));
		}

		public static async Task InsertSynchronized<T>(this ObservableCollection<T> collection, int index, T item)
		{
			await ClientObject.ExecuteSynchronized(() => collection.Insert(index, item));
		}

		public static async Task RemoveSynchronized<T>(this ObservableCollection<T> collection, T item)
		{
			await ClientObject.ExecuteSynchronized(() => collection.Remove(item));
		}
	}
}