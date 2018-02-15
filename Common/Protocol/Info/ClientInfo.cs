namespace SLD.Tezos.Protocol
{
	public class ClientInfo
	{
		// Identifies a distinct installation on a device
		public string InstanceID;

		// Version of the Tezos.Client.Protocol
		public int ProtocolVersion;

		// Platform of the device
		public TargetPlatform Platform;

		// Version of the client application
		public string ApplicationVersion;
	}
}