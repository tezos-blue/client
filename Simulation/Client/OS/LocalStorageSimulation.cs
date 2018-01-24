using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.OS
{
	using Simulation;
	using System;

	public class LocalStorageSimulation : IStoreLocal
	{
		internal static string instanceID = Guid.NewGuid().ToString();

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
			if (Parameters.Simulation != null)
			{
				return Task.FromResult(Parameters.Simulation.Identities.Select(i => MakeOutput(i)));
			}
			else
			{
				return Task.FromResult(new Stream[0] as IEnumerable<Stream>);
			}
		}

		public Task<string> GetInstanceID()
		{
			return Task.FromResult(instanceID);
		}

		private Stream MakeOutput(ClientObject item)
		{
			var output = new MemoryStream();
			var formatter = new BinaryFormatter();
			formatter.Serialize(output, item);
			output.Position = 0;

			return output;
		}
	}
}