namespace SLD.Tezos
{
	using Client.Model;
	using Protocol;
	using Simulation;

	public static class SimulationExtensions
	{
		public static void Notify(this TokenStore account, NetworkEvent netEvent)
			=> (account as IEventSource).Notify(netEvent);
	}
}