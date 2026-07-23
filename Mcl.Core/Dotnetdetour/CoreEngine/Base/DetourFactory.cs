using System;
using Mcl.Core.Dotnetdetour.CoreEngine.DetourWays;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;

namespace Mcl.Core.Dotnetdetour.CoreEngine.Base;

public class DetourFactory
{
    public static IDetour CreateDetourEngine()
    {
        if (IntPtr.Size == 4) return new NativeDetourFor32Bit();

        if (IntPtr.Size == 8) return new NativeDetourFor64Bit();

        throw new NotImplementedException();
    }
}