using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mcl.Core.Tools;

public static class HashExtensions
{
    public static byte[] ComputeFileHash(this MD5 md5, string filePath)
    {
        byte[] array;
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            array = md5.ComputeHash(fileStream);
        }

        return array;
    }
}
