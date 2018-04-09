using System.Threading.Tasks;

namespace SLD.Tezos
{
	public class SyncEvent<T>
	{
		private TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

		public Task<T> WhenComplete
			=> tcs.Task;

		public bool SetComplete(T result)
		{
			return tcs.TrySetResult(result);
		}
	}

	public class SyncEvent : SyncEvent<Result>
	{
		public bool SetComplete()
			=> SetComplete(Result.OK);

		public bool Timeout()
			=> SetComplete(Result.Timeout);

		public bool Cancel()
			=> SetComplete(Result.Cancelled);

		public bool Fail(string error = Result.GenericError)
			=> SetComplete(Result.Error(error));
	}
}