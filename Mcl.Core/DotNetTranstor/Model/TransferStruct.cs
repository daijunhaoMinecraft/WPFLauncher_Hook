using System.Runtime.InteropServices;

namespace Mcl.Core.DotNetTranstor.Model;

public class TransferStruct
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayerStateChange
    {
        public byte State;      // 对应 c.a (1=加入, 0=离开)
        
        public uint UserID;     // 对应 c.UserID
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TransfetLoginResult
    {
        public byte Result;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    // Token: 0x02000BD9 RID: 3033
    public struct PlayerCreateWebRtcConnectEvent
    {
        // Token: 0x04002630 RID: 9776
        public uint UserID;

        // Token: 0x04002631 RID: 9777
        public long PeerId;
    }
}