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
}