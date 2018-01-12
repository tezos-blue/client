//using System;
//using System.Threading.Tasks;
//using System.Collections.Generic;

//namespace SLD.Tezos.Client.Connections
//{
//    using Server.Model;
//    using Server.Protocol;

//    public class DirectConnection : Connection
//    {
//        Server.CloudEngine server = new Server.CloudEngine();

//        public DirectConnection()
//        {
//        }

//        public override async Task Connect(InstanceInfo registration)
//        {
//            Trace("-> Connect");
//            await server.Start();
//            Trace("<- Connected");
//        }

//        public override void Disconnect()
//        {
//            server.Stop();
//        }

//        public async override Task<CreateContractTask> PrepareCreateContract(CreateContractTask request)
//        {
//            Trace("-> Prepare Create Contract");
//            request = await server.PrepareCreateContract(request);
//            Trace("<- Contract Create prepared");

//            return request;
//        }

//        public async override Task<CreateContractTask> CreateContract(CreateContractTask contract)
//        {
//            Trace("-> Create Contract");
//            await server.ExecuteCreateContract(contract);
//            Trace("<- Contract created");

//            return contract;
//        }

//        public async override Task<decimal> GetBalance(string accountID)
//        {
//            return await server.GetBalance(accountID);
//        }

//        public override async Task<CreateFaucetTask> AlphaCreateFaucet(string managerID)
//        {
//            return await server.AlphaCreateFaucet(managerID);
//        }

//        public async override Task<AccountInfo> GetAccountInfo(string accountID)
//        {
//            return await server.GetAccountInfo(accountID);
//        }

//        //public override async Task<NetworkInfo> GetNetworkInfo()
//        //{
//        //    return await server.GetNetworkInfo();
//        //}

//        public override async Task<TransferTask> PrepareTransfer(TransferTask task)
//        {
//            Trace("-> Prepare Transfer");
//            task = await server.PrepareTransfer(task);
//            Trace("<- Transfer prepared");

//            return task;
//        }

//        public override async Task<TransferTask> Transfer(TransferTask task)
//        {
//            Trace("-> Execute Transfer");
//            await server.ExecuteTransfer(task);
//            Trace("<- Transfer executed");

//            return task;
//        }

//		public override Task Monitor(IEnumerable<string> accountIDs)
//		{
//			throw new NotImplementedException();
//		}
//	}
//}
