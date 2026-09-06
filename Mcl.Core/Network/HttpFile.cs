using System;
using System.IO;

namespace Mcl.Core.Network;

public class HttpFile
{
    public long ContentLength { get; set; }

    public Action<Stream> Writer { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public string Name { get; set; }
}