﻿using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Security
{
    using System;
    using Model;
    using OS;
	using System.Diagnostics;

	internal class LocalStore : TezosObject
    {
		public string InstanceID { get; private set; }

		#region Initialization

		private static LocalStore instance;

        private IStoreLocal osLocalStorage;

        private LocalStore(IStoreLocal osLocalStorage)
        {
            this.osLocalStorage = osLocalStorage;
        }

        internal async static Task<LocalStore> Open(IStoreLocal osLocalStorage)
        {
			Debug.Assert(osLocalStorage != null);

			if (instance == null)
            {
                instance = new LocalStore(osLocalStorage)
				{
					InstanceID = await osLocalStorage.GetInstanceID(),
				};
            }

            return instance;
        }

        #endregion Initialization

        private static BinaryFormatter formatter = new BinaryFormatter();

		internal async Task<IEnumerable<Identity>> LoadIdentities()
        {
            var identities = new List<Identity>();

            var files = await osLocalStorage.OpenIdentityFilesAsync();

            foreach (var file in files)
            {
				try
				{
					using (var input = await DecryptFile(file))
					{
						var identity = (Identity)formatter.Deserialize(input);

						identities.Add(identity);
					}
				}
				catch (Exception e)
				{
					Trace(e);
				}

                file.Dispose();
            }

            return identities;
        }

        internal async Task<IEnumerable<Account>> LoadAccounts()
        {
            var accounts = new List<Account>();

            var files = await osLocalStorage.OpenAccountFilesAsync();

            foreach (var file in files)
            {
                using (var input = await DecryptFile(file))
                {
                    var account = (Account)formatter.Deserialize(input);

                    accounts.Add(account);
                }

                file.Dispose();
            }

            return accounts;
        }

        internal async Task StoreAccount(Account account)
        {
            byte[] buffer;

            using (var output = new MemoryStream())
            {
                formatter.Serialize(output, account);

                buffer = output.ToArray();
            }

            using (var encrypted = new MemoryStream(buffer))
            using (var file = await osLocalStorage.CreateAccountFileAsync(account.AccountID))
            {
                await encrypted.CopyToAsync(file);
            }
        }

        internal async Task StoreIdentity(Identity identity)
        {
            byte[] buffer;

            using (var output = new MemoryStream())
            {
                formatter.Serialize(output, identity);

                buffer = output.ToArray();
            }

            using (var encrypted = new MemoryStream(buffer))
            using (var file = await osLocalStorage.CreateIdentityFileAsync(identity.AccountID))
            {
                await encrypted.CopyToAsync(file);
            }
        }

        private async Task<Stream> DecryptFile(Stream input)
        {
            var length = (int)input.Length;

            var buffer = new byte[length];

            await input.ReadAsync(buffer, 0, length);

            return new MemoryStream(buffer);
        }

        internal async Task DeleteAccount(Account account)
        {
			try
			{
				await osLocalStorage.DeleteAccountFileAsync(account.AccountID);
			}
			catch
			{
			}
        }
    }
}