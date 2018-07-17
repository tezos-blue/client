using System;
using System.Collections.Generic;
using System.Text;



/*
  
  module Prefix = struct

  (* 32 *)
  let block_hash = "\001\052" (* B(51) *)
  let operation_hash = "\005\116" (* o(51) *)
  let operation_list_hash = "\133\233" (* Lo(52) *)
  let operation_list_list_hash = "\029\159\109" (* LLo(53) *)
  let protocol_hash = "\002\170" (* P(51) *)

  (* 20 *)
  let ed25519_public_key_hash = "\006\161\159" (* tz1(36) *)

  (* 16 *)
  let cryptobox_public_key_hash = "\153\103" (* id(30) *)

  (* 32 *)
  let ed25519_public_key = "\013\015\037\217" (* edpk(54) *)

  (* 64 *)
  let ed25519_secret_key = "\043\246\078\007" (* edsk(98) *)
  let ed25519_signature = "\009\245\205\134\018" (* edsig(99) *)

  (* 4 *)
  let net_id = "\087\082\000" (* Net(15) *)

end

*/


namespace SLD.Tezos.Cryptography
{
    public class HashType
    {
        // 32
        public static readonly HashType Block = new HashType("Block", 32, new byte[]{ 1, 52 });
        public static readonly HashType Operation = new HashType("Operation", 32, new byte[]{ 5, 116 });
        public static readonly HashType OperationList = new HashType("OperationList", 32, new byte[]{ 133, 233 });
        public static readonly HashType OperationListList = new HashType("OperationListList", 32, new byte[]{ 29, 159, 109 });
        public static readonly HashType Protocol = new HashType("Protocol", 32, new byte[]{ 2, 170 });

		// 20
		public static readonly HashType PublicKeyHash = new HashType("PublicKeyHash", 20, new byte[] { 6, 161, 159 });
		public static readonly HashType PublicKeyHash2 = new HashType("PublicKeyHash2", 20, new byte[] { 6, 161, 161 });
		public static readonly HashType PublicKeyHash3 = new HashType("PublicKeyHash3", 20, new byte[] { 6, 161, 164 });

		public static readonly HashType Account = new HashType("Account", 20, new byte[] { 2, 90, 121 });

		// 16
		public static readonly HashType CryptoboxPublicKey = new HashType("CryptoboxPublicKey", 16, new byte[]{ 153, 103 });

        // 32
        public static readonly HashType Public = new HashType("Public", 32, new byte[]{ 13, 15, 37, 217 });

        // 64
        public static readonly HashType Private = new HashType("Private", 64, new byte[]{ 43, 246, 78, 7 });
        public static readonly HashType Signature = new HashType("Signature", 64, new byte[]{ 9, 245, 205, 134, 18 });

        // 4
        public static readonly HashType NetID = new HashType("NetID", 4, new byte[]{ 87, 82, 0 });

        #region Instance

        private HashType(string name, int size, byte[] prefix)
        {
            Name = name;
            Size = size;
            Prefix = prefix;
        }

        public string Name { get; private set; }

        public int Size { get; private set; }

        public byte[] Prefix { get; private set; }

        #endregion
    }
}
