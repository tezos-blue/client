namespace SLD.Tezos.Protocol
{
	public class IdentityAccountInfo
	{
		public string AccountID;

		public string Stereotype;
		public string Name;

		public decimal Balance;
	}

	public class IdentityInfo : IdentityAccountInfo
	{
		public IdentityAccountInfo[] Accounts;
	}
}