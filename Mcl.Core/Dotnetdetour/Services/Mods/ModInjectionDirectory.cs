using System.IO;

namespace Mcl.Core.Dotnetdetour.Services.Mods;

/// <summary>Directory preparation and discovery only; never launches Explorer or changes configuration.</summary>
internal static class ModInjectionDirectory
{
    // Preserve the existing working-directory contract used by the launcher and the game hook.
    internal static string GetPath() => Path.Combine(Directory.GetCurrentDirectory(), "ModsInject");

    internal static string EnsureExists()
    {
        var path = GetPath();
        Directory.CreateDirectory(path);
        return path;
    }

    internal static string[] GetJarFiles() => Directory.GetFiles(EnsureExists(), "*.jar");
}