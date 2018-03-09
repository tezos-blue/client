namespace SLD.Tezos.Protocol
{
	/// <summary>
	/// Current state of an account with regards to the freshness of its info
	/// </summary>
	public enum AccountState
	{
		/// <summary>
		/// Value not set
		/// </summary>
		Undefined,

		/// <summary>
		/// Not known in the system
		/// </summary>
		NotFound,

		/// <summary>
		/// Removed from blockchain, but still in the system
		/// </summary>
		Archived,

		/// <summary>
		/// In the blockchain, but current state from persisted data
		/// </summary>
		Cached,

		/// <summary>
		/// Active and synchronized with blockchain
		/// </summary>
		Live,
	}
}