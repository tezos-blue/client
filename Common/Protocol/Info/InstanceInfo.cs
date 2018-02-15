namespace SLD.Tezos.Protocol
{
	public class InstanceInfo
	{
		public string ApplicationVersion;
		public string InstanceID;
		public TargetPlatform Platform;
		public string PushServiceToken;

		// Deprecated in 0.3
		public string[] MonitoredAccounts;
	}
}