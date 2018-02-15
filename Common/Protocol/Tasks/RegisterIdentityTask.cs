namespace SLD.Tezos.Protocol
{
	public class RegisterIdentityTask : IdentityAccessTask
	{
		// Optional, for more access
		public string PublicKey;

		// Update metadata
		public string Name;
		public string Stereotype;

		// Return value
		public IdentityInfo Info;
	}
}