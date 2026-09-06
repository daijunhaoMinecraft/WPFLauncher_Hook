using System.Collections.Generic;
using System.Diagnostics;
using Mcl.Core.Network.Interface;

namespace Mcl.Core.Network;

[DebuggerDisplay("{DebuggerDisplay()}")]
public class NetResponse : NetResponseBase, INetResponse
{
    public NetResponse()
    {
        Headers = new List<Parameter>();
    }
}