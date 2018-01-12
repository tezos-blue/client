using System.IO;
using System.Runtime.Serialization;

namespace SLD.Tezos.Cryptography
{
    public abstract class Secret<T> where T : Secret<T>
    {
        byte[] encryptedData;

        protected Secret()
        {
        }

        protected Secret(SerializationInfo info, StreamingContext context)
        {
        }

        protected byte[] Data
        {
            get
            {
                return CryptoServices.DecryptUser(encryptedData);
            }

            set
            {
                encryptedData = CryptoServices.EncryptUser(value);
            }
        }

    }
}