using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace SLD.Tezos.Client.Security
{
    [Serializable]
    public abstract class Storable : ISerializable
    {
        protected Storable() { }
        protected Storable(SerializationInfo info, StreamingContext context)
        {

        }

        public abstract void GetObjectData(SerializationInfo info, StreamingContext context);

        //protected byte[] ReadData(Stream input)
        //{
        //    var reader = new StreamReader(input);

        //    var lengthBuffer = new byte[sizeof(UInt64)];

        //    reader.Read(lengthBuffer, 0, lengthBuffer.Length)
        //}

        //protected void WriteData(byte[] data, Stream output)
        //{
        //    var writer = new StreamWriter(output);

        //    UInt64 length = (UInt64)data.Length;

        //    writer.Write(length);

        //    writer.Write(data);
        //}
    }
}
