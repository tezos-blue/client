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
		// Create valid identities
		Task<Identity> CreateIdentity(string name, Passphrase passphrase, string stereotype = Identity.DefaultStereotype);

		Task<Identity> ImportIdentity(string name, KeyPair keyPair, string stereotype = Identity.DefaultStereotype);

		// Fill Metadata
		void RestoreIdentity(Identity identity);

		// Keep access to private key open
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

		public async Task<Identity> CreateIdentity(string name, Passphrase passphrase, string stereotype = Identity.DefaultStereotype)
		{
			if (passphrase == null) throw new ArgumentNullException("passphrase", "Identities must have a passphrase");

			// Create keys
			var keyPair = Guard.CreateKeyPair(passphrase);

			return await ImportIdentity(name, keyPair, stereotype);
		}

		public async Task<Identity> ImportIdentity(string name, KeyPair keyPair, string stereotype = Identity.DefaultStereotype)
		{
			if (keyPair == null) throw new ArgumentNullException(nameof(keyPair), "Identities must have keys");

			// Store in slot
			var slot = new Slot
			{
				Name = name,
				Stereotype = stereotype,
				Keys = keyPair,
			};

			await Store(slot);

			identities.Add(slot);

			// Create identity
			var identity = new Identity(keyPair.PublicKey, this)
			{
				Name = name,
				Stereotype = stereotype,
			};

			return identity;
		}

		public void RestoreIdentity(Identity identity)
		{
			// Find slot
			var slot = Find(identity.AccountID);

			if (slot != null)
			{
				identity.Name = slot.Name;
				identity.Stereotype = slot.Stereotype;
				identity.PublicKey = slot.Keys.PublicKey;
			}
		}

		public bool Unlock(string identityID, Passphrase passphrase)
		{
			if (passphrase == null) throw new ArgumentNullException(nameof(passphrase));

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
				try
				{
					using (stream)
					{
						var slot = (Slot)formatter.Deserialize(stream);

						identities.Add(slot);
					}
				}
				catch (SerializationException e)
				{
					// TODO Deprecated format, remove file from device
				}
				catch
				{
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
			public string Stereotype;

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
				Stereotype = info.GetString("Stereotype");

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
				info.AddValue("Stereotype", Stereotype);
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