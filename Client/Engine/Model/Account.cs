using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SLD.Tezos.Client.Model
{
	[Serializable]
	public class Account : TokenStore, ISerializable
	{
		public const string DefaultStereotype = "Account";

		private Account()
		{
			Stereotype = DefaultStereotype;
		}

		public Account(string name, string accountID = null) : this()
		{
			Name = name;
			_AccountID = accountID;
		}

		public override bool IsLive => Balance > 0;

		protected override Task<TokenStoreState> OnInitialize(Engine engine)
			=> RefreshInfo(engine);

		#region AccountID

		private string _AccountID;

		public override string AccountID => _AccountID;

		internal void SetAccountID(string accountID)
		{
			_AccountID = accountID;
			FirePropertyChanged(nameof(AccountID));
			FirePropertyChanged(nameof(Name));
		}

		#endregion AccountID

		#region Manager

		private Identity _Manager;

		public override Identity Manager => _Manager;

		public void SetManager(Identity manager)
		{
			_Manager = manager;
			FirePropertyChanged(nameof(Manager));
			FirePropertyChanged(nameof(ManagerID));
		}


		#endregion Manager

		#region Serialization

		public Account(SerializationInfo info, StreamingContext context) : this()
		{
			_AccountID = info.GetString("ID");
			Name = info.GetString("Name");
			storedManagerID = info.GetString("ManagerID");
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("ID", AccountID);
			info.AddValue("Name", Name);
			info.AddValue("ManagerID", ManagerID);
		}

		#endregion Serialization

	}
}