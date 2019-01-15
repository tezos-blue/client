namespace SLD.Tezos.Protocol
{
	public class GetAccountEntriesTask : BaseTask
	{
		public string AccountID;
		public string ManagerID;

		public AccountEntry[] Entries;
	}
}