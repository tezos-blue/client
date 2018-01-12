using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.OS
{
    public interface IStoreLocal
    {
        Task<IEnumerable<Stream>> OpenIdentityFilesAsync();
        Task<Stream> CreateIdentityFileAsync(string accountID);

        Task<IEnumerable<Stream>> OpenAccountFilesAsync();
        Task<Stream> CreateAccountFileAsync(string accountID);
        Task DeleteAccountFileAsync(string accountID);

		Task<string> GetInstanceID();
    }
}