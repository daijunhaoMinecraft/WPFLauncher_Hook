using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mcl.Core.Dotnetdetour.Models.Config;

internal sealed class BedrockCacheOptions
{
    internal bool Cleanup { get; private set; }
    internal bool Copy { get; private set; }
    internal string Root { get; private set; }
    internal string[] FileNames { get; private set; }
    internal string[] FolderNames { get; private set; }
    internal string[] Sources { get; private set; }
    internal int DurationSeconds { get; private set; }

    internal static bool IsCacheField(string field) => field == nameof(WpfConfig.EnableBedrockCacheCleanup) ||
        field == nameof(WpfConfig.EnableBedrockMcpCopy) || field == nameof(WpfConfig.BedrockPackcacheDirectory) ||
        field == nameof(WpfConfig.BedrockCacheFileNames) || field == nameof(WpfConfig.BedrockCacheFolderNames) ||
        field == nameof(WpfConfig.BedrockCacheDurationSeconds) || field == nameof(WpfConfig.BedrockMcpSources);

    internal static string[] Lines(string value) => (value ?? "")
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(line => line.Trim()).Where(line => line.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    internal static string FullPath(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) ||
                path.StartsWith("\\\\", StringComparison.Ordinal) ||
                (path.Length < 3 || path[1] != ':' || (path[2] != '\\' && path[2] != '/')))
                throw new ArgumentException("请输入本地绝对路径，不支持网络路径或设备路径。");
            if (path.IndexOf(':', 2) >= 0) throw new ArgumentException("路径不支持备用数据流。");
            var fullPath = Path.GetFullPath(path);
            if (fullPath.Length <= Path.GetPathRoot(fullPath).Length)
                throw new ArgumentException("不能选择整个磁盘作为缓存或资源来源。");
            return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception exception) when (exception is NotSupportedException || exception is PathTooLongException)
        {
            throw new ArgumentException("无效的缓存或资源路径。", exception);
        }
    }

    internal static bool IsWithin(string root, string path) =>
        path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    internal static void ValidateEntry(string field, object value)
    {
        if (field == nameof(WpfConfig.BedrockPackcacheDirectory))
        {
            var path = FullPath((string)value);
            if (!string.Equals(Path.GetFileName(path), "packcache", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("缓存目录必须是名称为 packcache 的文件夹，不能选择磁盘或游戏根目录。");
        }
        else if (field == nameof(WpfConfig.BedrockCacheFileNames) || field == nameof(WpfConfig.BedrockCacheFolderNames))
        {
            foreach (var name in Lines((string)value))
                if (name == "." || name == ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    name.EndsWith(".", StringComparison.Ordinal))
                    throw new ArgumentException("清理目标只能是精确名称，不允许路径、通配符、冒号或尾随句点。");
        }
        else if (field == nameof(WpfConfig.BedrockMcpSources))
        {
            foreach (var path in Lines((string)value)) FullPath(path);
        }
    }

    internal static void ValidateValues(IDictionary<string, object> values) => FromValues(values);

    internal static BedrockCacheOptions FromValues(IDictionary<string, object> values)
    {
        foreach (var field in new[]
                 {
                     nameof(WpfConfig.BedrockPackcacheDirectory), nameof(WpfConfig.BedrockCacheFileNames),
                     nameof(WpfConfig.BedrockCacheFolderNames), nameof(WpfConfig.BedrockMcpSources)
                 })
            ValidateEntry(field, values[field]);
        var options = new BedrockCacheOptions
        {
            Cleanup = (bool)values[nameof(WpfConfig.EnableBedrockCacheCleanup)],
            Copy = (bool)values[nameof(WpfConfig.EnableBedrockMcpCopy)],
            Root = FullPath((string)values[nameof(WpfConfig.BedrockPackcacheDirectory)]),
            FileNames = Lines((string)values[nameof(WpfConfig.BedrockCacheFileNames)]),
            FolderNames = Lines((string)values[nameof(WpfConfig.BedrockCacheFolderNames)]),
            Sources = Lines((string)values[nameof(WpfConfig.BedrockMcpSources)]).Select(FullPath).ToArray(),
            DurationSeconds = (int)values[nameof(WpfConfig.BedrockCacheDurationSeconds)]
        };
        if (options.Copy && options.Sources.Length == 0)
            throw new ArgumentException("开启 MCP 热复制前，请至少选择一个资源来源。");
        foreach (var source in options.Sources)
        {
            if (string.Equals(options.Root, source, StringComparison.OrdinalIgnoreCase) ||
                IsWithin(options.Root, source) || IsWithin(source, options.Root))
                throw new ArgumentException("资源来源与 packcache 目录不能相同、互相包含或重叠。");
            if (options.Copy && !Directory.Exists(source) &&
                !(File.Exists(source) &&
                  string.Equals(Path.GetExtension(source), ".mcp", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("MCP 来源必须是已存在的 .mcp 文件或文件夹：" + source);
        }

        return options;
    }
}