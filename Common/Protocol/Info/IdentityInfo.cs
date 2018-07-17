namespace SLD.Tezos.Protocol
{
	public class IdentityAccountInfo
	{
		public string AccountID;

		public string Stereotype;
		public string Name;

		public decimal Balance;

		public AccountState State;

		public string DelegateID;
	}

	public class IdentityInfo : IdentityAccountInfo
	{
		public IdentityAccountInfo[] Accounts;

		public bool IsUnknown
			=> State == AccountState.NotFound;
	}
}