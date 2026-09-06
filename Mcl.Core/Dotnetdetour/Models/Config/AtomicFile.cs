using System;
using System.IO;
using System.Text;

namespace Mcl.Core.Dotnetdetour.Models.Config;

internal static class AtomicFile
{
    /// <summary>Write beside the destination so replacement stays on the same volume.</summary>
    public static void WriteAllText(string path, string content)
    {
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, content, new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temporaryPath, path, null);
            else File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}