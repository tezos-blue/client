using System;
using System.Collections.Generic;
using System.Text;

namespace SLD.Tezos.Client.OS
{
    public interface IProtectMemory
    {
        byte[] Encrypt(byte[] data);
        byte[] Decrypt(byte[] encryptedData);
    }
}
