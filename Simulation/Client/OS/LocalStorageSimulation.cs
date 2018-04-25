using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.OS
{
	using Simulation;

	public class LocalStorageSimulation : IStoreLocal
	{
		public LocalStorageSimulation(SimulationParameters parameters = null)
		{
			Parameters = parameters ?? new SimulationParameters();
		}

		public SimulationParameters Parameters { get; private set; }

		public Task<Stream> CreateIdentityFileAsync(string accountID)
		{
			return Task.FromResult(new MemoryStream() as Stream);
		}

		public Task<IEnumerable<Stream>> OpenIdentityFilesAsync()
		{
			//if (Parameters.Simulation != null)
			//{
			//	return Task.FromResult(Parameters.Simulation.Identities.Select(i => CreateIdentitySlot(i)));
			//}
			//else
			//{
			//	return Task.FromResult(new Stream[0] as IEnumerable<Stream>);
			//}

			return Task.FromResult(new Stream[0] as IEnumerable<Stream>);
		}

		public Task DeleteIdentity(string identityID)
		{
			throw new NotImplementedException();
		}

		public Task PurgeAll()
		{
			throw new NotImplementedException();
		}

		//private Stream CreateIdentitySlot(SimulatedIdentity identity)
		//{
		//	var output = new MemoryStream();
		//	var formatter = new BinaryFormatter();

		//	var slot = new SoftwareVault.Slot
		//	{
		//		//Keys = identity.Keys,
		//		Name = identity.Name,
		//		Stereotype = identity.Stereotype,
		//	};

		//	formatter.Serialize(output, slot);
		//	output.Position = 0;

		//	return output;
		//}
	}
}