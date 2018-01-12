namespace SLD.Tezos
{
	using Client.Model;
	using Protocol;
	using Simulation;

	public static class SimulationExtensions
	{
		public static void Notify(this TokenStore account, NetworkEvent netEvent)
		{
			//await Task.Delay(50);

			(account as IEventSource).Notify(netEvent);
		}
	}
}