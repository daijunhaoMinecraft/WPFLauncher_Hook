using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

namespace Mcl.Core.Dotnetdetour.Services.Bedrock;

/// <summary>File-only operations. No process discovery, memory injection or host dependencies.</summary>
internal sealed class BedrockCacheWorker
{
    private const string LogModule = "BedrockCache";
    private readonly BedrockCacheOptions _options;
    private readonly HashSet<string> _copied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Action<Exception> _report;

    internal BedrockCacheWorker(BedrockCacheOptions options, Action<Exception> report)
    {
        _options = options;
        _report = report;
    }

    // Called on a background thread. Every mutation checks the owning process and current switch.
    // Returns true when this pass deleted or copied something, so the caller can back off while idle.
    internal bool RunPass(Func<bool> cleanupEnabled, Func<bool> copyEnabled, CancellationToken token)
    {
        if (!_options.Cleanup && !_options.Copy)
        {
            PluginLog.Debug(LogModule, "Pass skipped: cleanup and copy are both disabled.");
            return false;
        }

        if (!Directory.Exists(_options.Root))
        {
            // Launcher may create packcache after process start.
            PluginLog.Debug(LogModule, "Pass skipped: cache root does not exist: {0}.", _options.Root);
            return false;
        }

        EnsureSafePath(_options.Root, _options.Root);
        var changed = false;
        if (_options.Cleanup && cleanupEnabled())
            changed |= Cleanup(_options.Root, cleanupEnabled, token);
        if (!_options.Copy || !copyEnabled()) return changed;

        _copied.RemoveWhere(path => !Directory.Exists(path));
        var targets = Walk(_options.Root, token).Where(directory =>
            Directory.EnumerateFiles(directory).Any(file =>
                string.Equals(Path.GetExtension(file), ".mcp", StringComparison.OrdinalIgnoreCase))).ToArray();
        PluginLog.Debug(LogModule, "Pass found {0} MCP target(s).", targets.Length);
        // Do not feed copied subdirectories back into themselves on the next pass.
        foreach (var target in targets.Where(path =>
                     !targets.Any(parent => BedrockCacheOptions.IsWithin(parent, path))))
        {
            CheckActive(copyEnabled, token);
            if (_copied.Contains(target))
            {
                PluginLog.Debug(LogModule, "Skipping already-copied target: {0}.", target);
                continue;
            }

            try
            {
                foreach (var source in _options.Sources)
                {
                    CheckActive(copyEnabled, token);
                    EnsureSafePath(source, source);
                    if (Directory.Exists(source)) CopyDirectory(source, source, target, copyEnabled, token);
                    else CopyFile(source, source, Path.Combine(target, Path.GetFileName(source)), copyEnabled, token);
                }

                _copied.Add(target); // A failed/partial copy must be retried, not silently marked successful.
                changed = true;
                PluginLog.Info(LogModule, "Copied {0} source(s) into target: {1}.", _options.Sources.Length, target);
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                _report(exception);
            }
        }

        return changed;
    }

    private bool Cleanup(string directory, Func<bool> active, CancellationToken token)
    {
        CheckActive(active, token);
        EnsureSafePath(_options.Root, directory);
        var changed = false;
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            if (!_options.FileNames.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)) continue;
            try
            {
                CheckActive(active, token);
                EnsureSafePath(_options.Root, file);
                File.Delete(file);
                changed = true;
                PluginLog.Debug(LogModule, "Deleted cache file: {0}.", file);
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                _report(exception);
            }
        }

        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            try
            {
                CheckActive(active, token);
                EnsureSafePath(_options.Root, child);
                if (_options.FolderNames.Contains(Path.GetFileName(child), StringComparer.OrdinalIgnoreCase))
                {
                    DeleteDirectory(child, active, token);
                    changed = true;
                }
                else
                {
                    changed |= Cleanup(child, active, token);
                }
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                _report(exception);
            }
        }

        return changed;
    }

    private void DeleteDirectory(string directory, Func<bool> active, CancellationToken token)
    {
        CheckActive(active, token);
        EnsureSafePath(_options.Root, directory);
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            CheckActive(active, token);
            EnsureSafePath(_options.Root, file);
            File.Delete(file);
        }

        foreach (var child in Directory.EnumerateDirectories(directory)) DeleteDirectory(child, active, token);
        CheckActive(active, token);
        EnsureSafePath(_options.Root, directory);
        Directory.Delete(directory, false); // Never follow junctions through an unchecked recursive delete.
        PluginLog.Debug(LogModule, "Deleted cache directory: {0}.", directory);
    }

    // Manual repair path: empties the packcache root while keeping the folder itself.
    // Reuses the same boundary and reparse-point checks; unlike automated cleanup it keeps going
    // after an entry fails (e.g. a locked file), reporting each failure instead of aborting the pass.
    internal static int ClearContents(string root, Action<Exception> report)
    {
        root = BedrockCacheOptions.FullPath(root);
        EnsureSafePath(root, root);
        if (!Directory.Exists(root)) return 0;
        var removed = 0;
        foreach (var file in Directory.EnumerateFiles(root))
        {
            try
            {
                EnsureSafePath(root, file);
                File.Delete(file);
                removed++;
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                report(exception);
            }
        }

        foreach (var child in Directory.EnumerateDirectories(root))
        {
            try
            {
                EnsureSafePath(root, child);
                DeleteTree(root, child, report);
                removed++;
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                report(exception);
            }
        }

        return removed;
    }

    private static void DeleteTree(string root, string directory, Action<Exception> report)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            try
            {
                EnsureSafePath(root, file);
                File.Delete(file);
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                report(exception);
            }
        }

        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            try
            {
                EnsureSafePath(root, child);
                DeleteTree(root, child, report);
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                report(exception);
            }
        }

        try
        {
            Directory.Delete(directory, false); // Never follow junctions through an unchecked recursive delete.
        }
        catch (Exception exception) when (IsFileError(exception))
        {
            report(exception);
        }
    }

    private IEnumerable<string> Walk(string directory, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        EnsureSafePath(_options.Root, directory);
        yield return directory;
        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            try
            {
                EnsureSafePath(_options.Root, child);
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                _report(exception);
                continue;
            }

            foreach (var descendant in Walk(child, token)) yield return descendant;
        }
    }

    private void CopyDirectory(string sourceRoot, string source, string destination, Func<bool> active,
        CancellationToken token)
    {
        CheckActive(active, token);
        EnsureSafePath(sourceRoot, source);
        EnsureSafePath(_options.Root, destination);
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
            CopyFile(sourceRoot, file, Path.Combine(destination, Path.GetFileName(file)), active, token);
        foreach (var child in Directory.EnumerateDirectories(source))
            CopyDirectory(sourceRoot, child, Path.Combine(destination, Path.GetFileName(child)), active, token);
    }

    private void CopyFile(string sourceRoot, string source, string destination, Func<bool> active,
        CancellationToken token)
    {
        CheckActive(active, token);
        EnsureSafePath(sourceRoot, source);
        EnsureSafePath(_options.Root, destination);
        File.Copy(source, destination, true);
        PluginLog.Debug(LogModule, "Copied file: {0} -> {1}.", source, destination);
    }

    internal static void EnsureSafePath(string root, string path)
    {
        root = BedrockCacheOptions.FullPath(root);
        path = BedrockCacheOptions.FullPath(path);
        if (!string.Equals(root, path, StringComparison.OrdinalIgnoreCase) && !BedrockCacheOptions.IsWithin(root, path))
        {
            PluginLog.Debug(LogModule, "Refused path outside configured root: {0}.", path);
            throw new IOException("Refusing a path outside the configured root.");
        }

        // Check ancestors too: the selected root itself can be reached through a junction.
        for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                {
                    PluginLog.Debug(LogModule, "Refused symbolic link or junction: {0}.", current);
                    throw new IOException("Refusing a symbolic link or junction: " + current);
                }
            }
            catch (FileNotFoundException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }

    private static void CheckActive(Func<bool> active, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!active()) throw new OperationCanceledException();
    }

    internal static bool IsFileError(Exception exception) => exception is IOException ||
                                                             exception is UnauthorizedAccessException ||
                                                             exception is System.Security.SecurityException;
}