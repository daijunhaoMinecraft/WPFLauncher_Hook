using System;
using Mcl.Core.Dotnetdetour.DetourWays;

namespace Mcl.Core.Dotnetdetour
{
    public class DetourFactory
    {
        public static IDetour CreateDetourEngine()
        {
            if (IntPtr.Size == 4)
            {
                return new NativeDetourFor32Bit();
            }
            else if (IntPtr.Size == 8)
            {
                return new NativeDetourFor64Bit();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}