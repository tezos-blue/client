using System.Threading.Tasks;

namespace SLD.Tezos.Client.Flow
{
	public class Workflow : TezosObject
	{
		protected volatile SyncEvent syncCompleted = new SyncEvent();

		public Task<Result> WhenCompleted
			=> syncCompleted.WhenComplete;
	}
}