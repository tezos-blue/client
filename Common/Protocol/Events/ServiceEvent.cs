﻿namespace SLD.Tezos.Protocol
{
	public enum ServiceState
	{
		Unknown,
		Operational,
		Limited,
		Down
	}

	public class ServiceEvent : NetworkEvent
	{
		public ServiceState ServiceState;

		public string EventID;

		public string Data;
	}

	public static class ServiceEventID
	{
		public const string ServiceStateChanged = nameof(ServiceStateChanged);
		public const string CycleCleanup = nameof(CycleCleanup);
	}
}