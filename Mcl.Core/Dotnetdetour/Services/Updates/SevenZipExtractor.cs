using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

namespace Mcl.Core.Updater
{
    /// <summary>
    /// 官方补丁解压的移植版本（对应 MCLauncherUpdater 的 SevenZipHelper）。
    /// 使用启动器安装目录自带的 ext\7z\7z.exe，命令行与官方一致：
    /// x "包.7z" -y -aoa -o"目标目录"，另加官方没有的 -xr!排除项（保留钩子自己的 Mcl.Core.dll）。
    /// </summary>
    public static class SevenZipExtractor
    {
        public const int Ok = 0;
        public const int ProcessError = -1;
        public const int NotZipFiles = -2;
        public const int ArchiveNotFound = -3;
        public const int Cancelled = -4;
        public const int SevenZipNotFound = -5;

        private static readonly TimeSpan ExtractTimeout = TimeSpan.FromMinutes(30);

        public static string ResolveSevenZipPath(string launcherDirectory)
        {
            var root = string.IsNullOrWhiteSpace(launcherDirectory)
                ? Directory.GetCurrentDirectory()
                : launcherDirectory;
            return Path.Combine(root, "ext", "7z", "7z.exe");
        }

        public static string GetMessage(int code)
        {
            switch (code)
            {
                case Ok: return "成功";
                case ProcessError: return "解压进程执行失败";
                case NotZipFiles: return "更新包内容不完整";
                case ArchiveNotFound: return "更新包不存在";
                case Cancelled: return "已取消";
                case SevenZipNotFound: return "未找到 ext\\7z\\7z.exe，请检查启动器安装是否完整";
                case 1: return "解压完成但存在警告";
                case 2: return "解压发生致命错误";
                case 7: return "解压命令行错误";
                case 8: return "内存不足，解压失败";
                default: return "解压失败（退出码 " + code + "）";
            }
        }

        /// <summary>解压更新包。<paramref name="excludeNames"/> 中的文件名在任意层级都不会被解压。</summary>
        public static int Extract(
            string sevenZipPath,
            string archivePath,
            string targetDirectory,
            IReadOnlyCollection<string> excludeNames,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(sevenZipPath))
            {
                PluginLog.Error("Update", "未找到 7z.exe: " + sevenZipPath);
                return SevenZipNotFound;
            }

            if (!File.Exists(archivePath))
            {
                PluginLog.Error("Update", "更新包不存在: " + archivePath);
                return ArchiveNotFound;
            }

            try
            {
                Directory.CreateDirectory(targetDirectory);
            }
            catch (Exception ex)
            {
                PluginLog.Error("Update", ex, "创建解压目录失败: " + targetDirectory);
                return ProcessError;
            }

            var arguments = new StringBuilder();
            arguments.Append("x \"").Append(archivePath).Append("\" -y -aoa -o\"").Append(targetDirectory).Append('"');
            if (excludeNames != null)
            {
                foreach (var name in excludeNames)
                {
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        arguments.Append(" -xr!").Append(name);
                    }
                }
            }

            PluginLog.Debug("Update", "7z " + arguments);

            var output = new StringBuilder();
            var error = new StringBuilder();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(sevenZipPath, arguments.ToString())
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(sevenZipPath)) ?? string.Empty
                }
            };

            try
            {
                process.OutputDataReceived += (_, args) => AppendLimited(output, args.Data);
                process.ErrorDataReceived += (_, args) => AppendLimited(error, args.Data);

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                var deadline = DateTime.UtcNow + ExtractTimeout;
                while (!process.WaitForExit(500))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        TryKill(process);
                        return Cancelled;
                    }

                    if (DateTime.UtcNow > deadline)
                    {
                        TryKill(process);
                        PluginLog.Error("Update", "解压超时，已终止 7z 进程: " + archivePath);
                        return ProcessError;
                    }
                }

                // 等待异步输出流收尾，保证日志完整。
                process.WaitForExit();
                var exitCode = process.ExitCode;
                if (exitCode != Ok)
                {
                    PluginLog.Error("Update",
                        $"7z 解压失败({exitCode}): {Path.GetFileName(archivePath)}，输出: {error} {output}");
                }

                return exitCode;
            }
            catch (Exception ex)
            {
                PluginLog.Error("Update", ex, "运行 7z 解压失败: " + archivePath);
                TryKill(process);
                return ProcessError;
            }
            finally
            {
                process.Dispose();
            }
        }

        private static void AppendLimited(StringBuilder builder, string line)
        {
            if (string.IsNullOrEmpty(line) || builder.Length > 8192)
            {
                return;
            }

            builder.AppendLine(line);
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                PluginLog.Debug("Update", "终止 7z 进程失败: " + ex.Message);
            }
        }
    }
}
