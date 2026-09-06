using System;
using System.IO;

namespace Mcl.Core.Network;

public class FileParameter
{
    public long ContentLength { get; set; }

    public Action<Stream> Writer { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public string Name { get; set; }

    public static FileParameter Create(string name, byte[] data, string filename, string contentType)
    {
        var num = (long)data.Length;
        return new FileParameter
        {
            Writer = delegate(Stream s) { s.Write(data, 0, data.Length); },
            FileName = filename,
            ContentType = contentType,
            ContentLength = num,
            Name = name
        };
    }

    public static FileParameter Create(string name, byte[] data, string filename)
    {
        return Create(name, data, filename, null);
    }
}