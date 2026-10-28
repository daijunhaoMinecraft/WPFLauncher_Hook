using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

namespace Mcl.Core.Updater
{
    /// <summary>一次已下载完成、等待宿主退出后应用的更新。</summary>
    public sealed class DeferredPatchApplyRequest
    {
        public int HostProcessId { get; set; }

        /// <summary>用户最初启动的启动器可执行文件（更新完成后重新启动它）。</summary>
        public string HostExecutablePath { get; set; }

        public string LauncherDirectory { get; set; }

        /// <summary>存放下载包、解压结果与 apply 脚本的会话目录。</summary>
        public string SessionDirectory { get; set; }

        /// <summary>大版本整包安装器；没有大版本步骤时为 null。</summary>
        public string MajorInstallerPath { get; set; }

        /// <summary>小版本补丁的解压目录；没有小版本步骤时为 null。</summary>
        public string MinorPatchDirectory { get; set; }
    }

    /// <summary>
    /// 更新应用器。钩子运行在启动器进程内部，直接覆盖被占用的文件会失败，
    /// 因此这里生成一个独立的 cmd 脚本：等待当前启动器进程退出后再覆盖文件并重新启动启动器，
    /// 与 MCLauncherUpdater 的 KillAllProcessByName + CopyDir 流程等价。
    /// 脚本解压/拷贝阶段都会排除钩子自己的 Mcl.Core.dll，避免被官方同名的原版文件覆盖。
    /// </summary>
    public static class DeferredPatchApplier
    {
        public const string HookAssemblyName = "Mcl.Core.dll";
        public const string HookSymbolName = "Mcl.Core.pdb";
        public const string ScriptFileName = "apply-update.cmd";
        public const string LogFileName = "apply-update.log";

        public static string BuildScript(DeferredPatchApplyRequest request)
        {
            Validate(request);

            var hookPath = Path.Combine(request.LauncherDirectory, HookAssemblyName);
            var backupPath = Path.Combine(request.SessionDirectory, HookAssemblyName + ".update-backup");
            var logPath = Path.Combine(request.SessionDirectory, LogFileName);
            var builder = new StringBuilder();

            // 脚本保持纯 ASCII 输出，避免宿主在非 Unicode 代码页下解析失败。
            builder.AppendLine("@echo off");
            builder.AppendLine("setlocal EnableExtensions");
            builder.AppendLine("set \"SESSION=" + request.SessionDirectory + "\"");
            builder.AppendLine("set \"INSTALL=" + request.LauncherDirectory + "\"");
            builder.AppendLine("set \"HOOK=" + hookPath + "\"");
            builder.AppendLine("set \"BACKUP=" + backupPath + "\"");
            builder.AppendLine("set \"LOG=" + logPath + "\"");
            builder.AppendLine();
            builder.AppendLine(">>\"%LOG%\" echo [%date% %time%] waiting for host process "
                               + request.HostProcessId + " to exit");
            builder.AppendLine(":wait_host");
            builder.AppendLine("tasklist /FI \"PID eq " + request.HostProcessId + "\" /NH 2>nul | find \""
                               + request.HostProcessId + "\" >nul");
            builder.AppendLine("if not errorlevel 1 (");
            builder.AppendLine("  >nul ping -n 2 127.0.0.1");
            builder.AppendLine("  goto wait_host");
            builder.AppendLine(")");
            builder.AppendLine(">nul ping -n 3 127.0.0.1");
            builder.AppendLine(">>\"%LOG%\" echo [%date% %time%] host exited, applying update");
            builder.AppendLine();
            builder.AppendLine("if exist \"%HOOK%\" copy /Y \"%HOOK%\" \"%BACKUP%\" >nul 2>nul");

            if (!string.IsNullOrEmpty(request.MajorInstallerPath))
            {
                builder.AppendLine();
                builder.AppendLine(">>\"%LOG%\" echo [%date% %time%] running installer: "
                                   + Path.GetFileName(request.MajorInstallerPath));
                builder.AppendLine("start \"\" /wait \"" + request.MajorInstallerPath
                                   + "\" /silent /verisilent /norestart /nocancel");
                builder.AppendLine(">>\"%LOG%\" echo [%date% %time%] installer exit code %ERRORLEVEL%");
            }

            if (!string.IsNullOrEmpty(request.MinorPatchDirectory))
            {
                builder.AppendLine();
                builder.AppendLine(">>\"%LOG%\" echo [%date% %time%] copying patch files");
                builder.AppendLine("robocopy \"" + request.MinorPatchDirectory + "\" \"%INSTALL%\" /E /COPY:DAT /R:2 /W:1"
                                   + " /XF " + HookAssemblyName + " " + HookSymbolName
                                   + " /NFL /NDL /NP /LOG+:\"%LOG%\"");
                builder.AppendLine("set \"RC=%ERRORLEVEL%\"");
                builder.AppendLine(">>\"%LOG%\" echo [%date% %time%] robocopy exit code %RC%");
                builder.AppendLine("if %RC% GEQ 8 (");
                builder.AppendLine("  >>\"%LOG%\" echo [%date% %time%] WARNING: robocopy reported failures");
                builder.AppendLine(")");
            }

            builder.AppendLine();
            builder.AppendLine("if exist \"%BACKUP%\" copy /Y \"%BACKUP%\" \"%HOOK%\" >nul 2>nul");
            builder.AppendLine(">>\"%LOG%\" echo [%date% %time%] hook assembly preserved: %HOOK%");
            builder.AppendLine("pushd \"%INSTALL%\"");
            builder.AppendLine("start \"\" \"" + request.HostExecutablePath + "\"");
            builder.AppendLine("popd");
            builder.AppendLine(">>\"%LOG%\" echo [%date% %time%] launcher restarted, apply finished");
            builder.AppendLine("endlocal");
            builder.AppendLine("exit /b 0");
            return builder.ToString();
        }

        /// <summary>写出脚本并异步启动。调用方随后应尽快退出宿主进程。</summary>
        public static bool Start(DeferredPatchApplyRequest request, out string scriptPath, out string logPath)
        {
            Validate(request);
            scriptPath = Path.Combine(request.SessionDirectory, ScriptFileName);
            logPath = Path.Combine(request.SessionDirectory, LogFileName);

            try
            {
                Directory.CreateDirectory(request.SessionDirectory);
                // cmd.exe 按系统 ANSI 代码页读取批处理，使用 Default 编码保留中文用户目录。
                File.WriteAllText(scriptPath, BuildScript(request), Encoding.Default);

                var startInfo = new ProcessStartInfo("cmd.exe", "/c \"\"" + scriptPath + "\"\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = request.SessionDirectory
                };
                Process.Start(startInfo);
                PluginLog.Info("Update", "已安排更新应用脚本: " + scriptPath);
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Error("Update", ex, "启动更新应用脚本失败: " + scriptPath);
                return false;
            }
        }

        private static void Validate(DeferredPatchApplyRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.LauncherDirectory))
            {
                throw new ArgumentException("启动器目录不能为空", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.SessionDirectory))
            {
                throw new ArgumentException("会话目录不能为空", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.HostExecutablePath))
            {
                throw new ArgumentException("重启目标不能为空", nameof(request));
            }

            if (string.IsNullOrEmpty(request.MajorInstallerPath) && string.IsNullOrEmpty(request.MinorPatchDirectory))
            {
                throw new ArgumentException("没有需要应用的更新内容", nameof(request));
            }
        }
    }
}
