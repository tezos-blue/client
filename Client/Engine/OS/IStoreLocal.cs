using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.OS
{
	public interface IStoreLocal
	{
		Task<IEnumerable<Stream>> OpenIdentityFilesAsync();

		Task<Stream> CreateIdentityFileAsync(string identityID);

		Task DeleteIdentity(string identityID);

		Task PurgeAll();
	}
}