using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Mcl.Core.Updater
{
    /// <summary>官方补丁列表中的单个版本条目。</summary>
    public sealed class LauncherPatchInfo
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("md5")]
        public string Md5 { get; set; }
    }

    /// <summary>
    /// 启动器更新渠道映射。渠道名来自 <see cref="Mcl.Core.Tools.Cryptography.Channel.ChannelGet"/>，
    /// 这里只保留 MCLauncherUpdater 的 UpdateConfig 中与更新流程有关的两个前缀规则：
    /// 补丁列表前缀使用 ChannelPrefix 字典（a50 为 A50SdkCn_），产品名使用 GetChannelPrefix（a50 为空）。
    /// </summary>
    public sealed class LauncherChannelProfile
    {
        public const string BaseUrl = "https://x19.update.netease.com";
        public const string NoticePrefix = "/MCUpdate_";
        public const string PatchListFileName = "x19_java_patchlist";

        public static readonly LauncherChannelProfile Netease =
            new LauncherChannelProfile("netease", "网易官方", string.Empty, "WPFLauncher");

        public static readonly LauncherChannelProfile Pc4399 =
            new LauncherChannelProfile("4399pc", "4399 平台", "PC4399_", "PC4399_WPFLauncher");

        public static readonly LauncherChannelProfile A50SdkCn =
            new LauncherChannelProfile("a50_sdk_cn", "A50 SDK", "A50SdkCn_", "WPFLauncher");

        private LauncherChannelProfile(string channel, string displayName, string patchListPrefix, string productName)
        {
            Channel = channel;
            DisplayName = displayName;
            PatchListPrefix = patchListPrefix;
            ProductName = productName;
        }

        public string Channel { get; }

        public string DisplayName { get; }

        /// <summary>官方 /pl/{prefix}x19_java_patchlist 中的渠道前缀。</summary>
        public string PatchListPrefix { get; }

        /// <summary>安装目录中的启动器可执行文件名（不含扩展名）。</summary>
        public string ProductName { get; }

        public string BuildPatchListPath()
        {
            return "/pl/" + PatchListPrefix + PatchListFileName;
        }

        public static LauncherChannelProfile FromChannel(string channel)
        {
            switch (channel)
            {
                case "4399pc":
                    return Pc4399;
                case "a50_sdk_cn":
                    return A50SdkCn;
                default:
                    return Netease;
            }
        }
    }

    /// <summary>官方 patchlist 响应解析。响应体缺少最外层花括号且允许尾随逗号。</summary>
    public static class LauncherPatchList
    {
        public static Dictionary<string, LauncherPatchInfo> Parse(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return new Dictionary<string, LauncherPatchInfo>();
            }

            var body = payload.Trim().TrimEnd(',');
            return JsonConvert.DeserializeObject<Dictionary<string, LauncherPatchInfo>>("{" + body + "}")
                   ?? new Dictionary<string, LauncherPatchInfo>();
        }
    }

    /// <summary>
    /// 更新计划：由当前版本与官方版本列表计算出的实际下载步骤。
    /// 对齐 MCLauncherUpdater.UpdateManager 的 GetLastMajorMinorPatch/DownloadUpdate：
    /// 列表中最新的 Major.Minor 系列的首个版本作为大版本整包，列表最后一项作为小版本补丁；
    /// 只有高于本机当前版本的步骤才会执行。
    /// </summary>
    public sealed class LauncherUpdatePlan
    {
        private LauncherUpdatePlan(
            Version currentVersion,
            Version newestVersion,
            Version majorVersion,
            LauncherPatchInfo majorPatch,
            Version minorVersion,
            LauncherPatchInfo minorPatch)
        {
            CurrentVersion = currentVersion;
            NewestVersion = newestVersion;
            MajorVersion = majorVersion;
            MajorPatch = majorPatch;
            MinorVersion = minorVersion;
            MinorPatch = minorPatch;
        }

        public Version CurrentVersion { get; }

        public Version NewestVersion { get; }

        public Version MajorVersion { get; }

        public LauncherPatchInfo MajorPatch { get; }

        public Version MinorVersion { get; }

        public LauncherPatchInfo MinorPatch { get; }

        public bool HasUpdate => MajorVersion != null || MinorVersion != null;

        public int PackageCount => (MajorVersion != null ? 1 : 0) + (MinorVersion != null ? 1 : 0);

        public long TotalDownloadSize => (MajorPatch?.Size ?? 0) + (MinorPatch?.Size ?? 0);

        public static LauncherUpdatePlan Create(Version currentVersion, IDictionary<string, LauncherPatchInfo> versions)
        {
            var empty = new LauncherUpdatePlan(currentVersion ?? new Version("0.0.0.0"), null, null, null, null, null);
            if (versions == null || versions.Count == 0)
            {
                return empty;
            }

            var sorted = new List<Version>();
            foreach (var key in versions.Keys)
            {
                if (Version.TryParse(key, out var parsed))
                {
                    sorted.Add(parsed);
                }
            }

            if (sorted.Count == 0)
            {
                return empty;
            }

            sorted.Sort();
            var newest = sorted[sorted.Count - 1];

            var major = newest;
            var minor = newest;
            var index = sorted.Count - 2;
            while (index >= 0 && sorted[index].Major == major.Major && sorted[index].Minor == major.Minor)
            {
                major = sorted[index];
                index--;
            }

            if (minor == major)
            {
                minor = null;
            }

            if (currentVersion != null)
            {
                if (major != null && major <= currentVersion)
                {
                    major = null;
                }

                if (minor != null && minor <= currentVersion)
                {
                    minor = null;
                }
            }

            return new LauncherUpdatePlan(
                currentVersion ?? new Version("0.0.0.0"),
                newest,
                major,
                major != null && versions.TryGetValue(major.ToString(), out var majorInfo) ? majorInfo : null,
                minor,
                minor != null && versions.TryGetValue(minor.ToString(), out var minorInfo) ? minorInfo : null);
        }
    }
}
