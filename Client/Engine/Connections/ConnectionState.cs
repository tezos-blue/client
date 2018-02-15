using System;
using System.Collections.Generic;
using System.Text;

namespace SLD.Tezos.Client.Connections
{
    public enum ConnectionState
    {
        Unknown,
        Disconnected,
        Connecting,
        Connected,
		Online
    }
}
