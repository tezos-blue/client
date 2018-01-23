using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Security
{
	using Cryptography;
	using Model;
	using OS;

	public interface IManageIdentities
	{
		Task<Identity> CreateIdentity(string name, Passphrase passphrase);

		bool Unlock(string identityID, Passphrase passphrase);

		void Lock(string identityID);

		bool IsUnlocked(string identityID);
	}

	public class SoftwareVault : IManageIdentities, IProvideSigning
	{
		private static BinaryFormatter formatter = new BinaryFormatter();
		private IStoreLocal localStorage;

		private List<Slot> identities = new List<Slot>();

		public SoftwareVault(IStoreLocal localStorage)
		{
			this.localStorage = localStorage;
		}

		#region IManageIdentities

		public async Task<Identity> CreateIdentity(string name, Passphrase passphrase)
		{
			if (passphrase == null) throw new ArgumentNullException("passphrase", "Identities must have a passphrase");

			// Create keys
			var keyPair = Guard.CreateKeyPair(passphrase);

			// Store in slot
			var slot = new Slot
			{
				Name = name,
				Keys = keyPair,
			};

			await Store(slot);

			identities.Add(slot);

			// Create identity
			var identity = new Identity(keyPair.PublicKey, this)
			{
				Name = name,
			};

			return identity;
		}

		public bool Unlock(string identityID, Passphrase passphrase)
		{
			var identity = Get(identityID);

			return identity.Unlock(passphrase);
		}

		public void Lock(string identityID)
		{
			var identity = Get(identityID);

			identity.Lock();
		}

		public bool IsUnlocked(string identityID)
		{
			var identity = Get(identityID);

			return identity.IsUnlocked;
		}

		#endregion IManageIdentities

		#region IProvideSigning

		public IEnumerable<string> IdentityIDs => identities.Select(i => i.IdentityID);

		public Task<bool> Sign(string identityID, byte[] data, out byte[] signature)
		{
			signature = null;

			// Find identity
			var identity = Find(identityID);

			if (identity != null)
			{
				if (identity.IsUnlocked)
				{
					// Sign
					signature = CryptoServices.CreateSignature(identity.PrivateKeyData, data);

					return Task.FromResult(true);
				}
			}

			return Task.FromResult(false);
		}

		public byte[] GetPublicKey(string identityID)
		{
			var slot = Find(identityID);

			return slot?.Keys.PublicKey.Data;
		}

		public bool Contains(string identityID) => identities.Any(i => i.IdentityID == identityID);

		public async Task Initialize()
		{
			await InitializeIdentities();
		}

		#endregion IProvideSigning

		#region Identities

		private Slot Find(string identityID) => identities.FirstOrDefault(i => i.IdentityID == identityID);

		private Slot Get(string identityID) => Find(identityID) ?? throw new KeyNotFoundException($"Could not find {identityID}");

		private async Task InitializeIdentities()
		{
			var identityStreams = await localStorage.OpenIdentityFilesAsync();

			var tasks = identityStreams.Select(stream =>
			{
				using (stream)
				{
					var slot = (Slot)formatter.Deserialize(stream);

					identities.Add(slot);
				}

				return Task.CompletedTask;
			});

			await Task.WhenAll(tasks);
		}

		private async Task Store(Slot slot)
		{
			using (var stream = await localStorage.CreateIdentityFileAsync(slot.IdentityID))
			{
				formatter.Serialize(stream, slot);
			}
		}

		[Serializable]
		public class Slot : TezosObject, ISerializable
		{
			public string Name;
			public KeyPair Keys;

			private Passphrase passphrase;

			public Slot()
			{
			}

			public string IdentityID => Keys.PublicID;

			internal byte[] PrivateKeyData
			{
				get
				{
					if (!IsUnlocked)
					{
						throw new InvalidOperationException($"Identity {IdentityID} is locked.");
					}

					return Keys.PrivateKey.AccessData(passphrase);
				}
			}

			#region Lock

			internal bool IsUnlocked => passphrase != null;

			internal void Lock()
			{
				if (passphrase != null)
				{
					passphrase.Dispose();
					passphrase = null;
				}
			}

			internal bool Unlock(Passphrase passphrase)
			{
				if (Keys.CanUnlockWith(passphrase))
				{
					this.passphrase = new Passphrase(passphrase);

					return true;
				}
				else
				{
					Lock();

					return false;
				}
			}

			#endregion Lock

			#region Serialization

			public Slot(SerializationInfo info, StreamingContext context) : this()
			{
				Name = info.GetString("Name");

				Keys = new KeyPair();

				Keys.PublicKey = (PublicKey)info.GetValue("PublicKey", typeof(PublicKey));

				var encryptedPrivate = (byte[])info.GetValue("PrivateKey", typeof(byte[]));

				if (encryptedPrivate != null)
				{
					Keys.PrivateKey = PrivateKey.Restore(encryptedPrivate);
				}
			}

			public void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				info.AddValue("Name", Name);
				info.AddValue("PublicKey", Keys.PublicKey, typeof(PublicKey));

				if (Keys.PrivateKey != null)
				{
					info.AddValue("PrivateKey", Keys.PrivateKey.EncryptedData, typeof(byte[]));
				}
			}

			#endregion Serialization
		}

		#endregion Identities
	}
}