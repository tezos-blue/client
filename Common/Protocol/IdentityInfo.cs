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

		// By convention, a null array means the identity has not been seen on the chain before
		public bool IsUnknown => Accounts == null;
	}
}