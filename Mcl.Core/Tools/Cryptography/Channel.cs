using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mcl.Core.Utils;
using Microsoft.Win32;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

namespace Mcl.Core.Tools.Cryptography;

public class Channel
{
    private const string LogModule = "ChannelSystem";
    public static string CurrentChannel = "";

    // 静态构造函数：处理 Mono.Cecil 内嵌资源的释放
    static Channel()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            if (args.Name.Contains("Mono.Cecil"))
            {
                string resourceName = "Mcl.Core.DLLs.Mono.Cecil.dll";
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        byte[] data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);
                        return Assembly.Load(data);
                    }
                    PluginLog.Error(LogModule, $"Failed to extract embedded resource: {resourceName}");
                }
            }
            return null;
        };
    }

    public static string ChannelGet()
    {
        if (string.IsNullOrWhiteSpace(CurrentChannel))
        {
            string channel = null;
            string updaterPath =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater", "MCLauncherUpdater.exe");

            // 1. 尝试从 Updater 静态解析
            if (File.Exists(updaterPath))
            {
                try
                {
                    channel = ParseChannelUsingCecil(updaterPath);
                }
                catch (Exception ex)
                {
                    PluginLog.Warn(LogModule,
                        $"Failed to parse ChannelName from Updater. Fallback to local files. Error: {ex.Message}");
                }
            }
            else
            {
                PluginLog.Warn(LogModule, "MCLauncherUpdater.exe not found. Fallback to local files.");
            }

            // 2. 如果解析成功，直接返回
            if (!string.IsNullOrEmpty(channel))
            {
                PluginLog.Info(LogModule, $"Successfully acquired channel '{channel}' from Updater.");
                return channel;
            }

            // 3. 回退到传统的本地文件判断
            PluginLog.Debug(LogModule, "Using traditional local file check for channel.");
            if (File.Exists("4399pc.data")) channel = "4399pc";
            else if (File.Exists("native_a50_cn.data")) channel = "a50_sdk_cn";
            else channel = "netease";
            CurrentChannel = channel;
            return channel;
        }
        return CurrentChannel;
    }

    // 核心解析逻辑 (NoInlining 特性绝对不能删，防止过早触发程序集加载)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string ParseChannelUsingCecil(string exePath)
    {
        using (var module = Mono.Cecil.ModuleDefinition.ReadModule(exePath))
        {
            var targetType = module.GetType("Update.Config.UpdateConfig");
            if (targetType == null) return null;

            var field = targetType.Fields.FirstOrDefault(f => f.Name == "ChannelName");
            if (field == null) return null;

            if (field.HasConstant) return field.Constant?.ToString();

            var cctor = targetType.Methods.FirstOrDefault(m => m.Name == ".cctor");
            if (cctor != null && cctor.HasBody)
            {
                var instructions = cctor.Body.Instructions;
                for (int i = 1; i < instructions.Count; i++)
                {
                    if (instructions[i].OpCode == Mono.Cecil.Cil.OpCodes.Stsfld &&
                        instructions[i].Operand is Mono.Cecil.FieldReference fr && 
                        fr.Name == "ChannelName")
                    {
                        if (instructions[i - 1].OpCode == Mono.Cecil.Cil.OpCodes.Ldstr)
                        {
                            return instructions[i - 1].Operand?.ToString();
                        }
                    }
                }
            }
        }
        return null;
    }

    public static void InitializeRegistry()
    {
        string channel = ChannelGet();
        string registrySubKey = "SOFTWARE\\Netease\\";
        
        switch (channel)
        {
            case "4399pc":
                registrySubKey += "PC4399_";
                break;
            case "a50_sdk_cn":
                registrySubKey += "A50SdkCn_";
                break;
        }
        
        registrySubKey += "MCLauncher";
        
        try
        {
            RegistryHelper.InitRegistryKey(registrySubKey);
            PluginLog.Info(LogModule, $"Registry initialized for channel '{channel}' at '{registrySubKey}'.");
        }
        catch (Exception ex)
        {
            PluginLog.Error(LogModule, ex, $"Failed to initialize registry for '{registrySubKey}'.");
        }
    }
}