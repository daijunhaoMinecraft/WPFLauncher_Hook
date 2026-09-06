using System;
using System.Security.Cryptography;

namespace Mcl.Core.Tools;

public static class HashHelper
{
    // MD5 is required by the host protocol; it is not suitable for password storage.
    public static string ComputeMD5(byte[] bytes)
    {
        if (bytes == null) return null;
        try
        {
            using (var md5 = MD5.Create()) return md5.ComputeHash(bytes).ToHex();
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}