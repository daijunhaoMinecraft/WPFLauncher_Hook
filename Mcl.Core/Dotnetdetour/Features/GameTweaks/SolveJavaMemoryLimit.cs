using System;
using System.Reflection;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Features.GeneralHooks;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Common;
using WPFLauncher.Common;
using WPFLauncher.Manager.Configuration;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

// 解决Java版最大内存只能设置到8191问题
public class SolveJavaMemoryLimit : IMethodHook
{
    [HookMethod("WPFLauncher.ViewModel.SysSetting.ho", "b")]
    public static void ModifyMemorySettings(object targetInstance)
    {
        if (targetInstance == null)
        {
            PluginLog.Info("Game", "错误：未找到 SysSetting 实例。");
            return;
        }

        // 1. 获取对象的类型
        var type = targetInstance.GetType();

        // 确认类型名称是否匹配（可选，用于调试）
        // WpfConfig.DefaultLogger.Info($"Target Type: {type.FullName}");

        // 2. 定义 BindingFlags：非公开 (NonPublic) + 实例 (Instance)
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        try
        {
            // 3. 获取 hs 字段
            var fieldHs = type.GetField("hs", flags);
            if (fieldHs == null)
            {
                PluginLog.Error("Game", "未找到字段 'hs'，可能混淆后的名称已改变。");
                return;
            }

            // 4. 获取 ht 字段
            var fieldHt = type.GetField("ht", flags);
            if (fieldHt == null)
            {
                PluginLog.Error("Game", "未找到字段 'ht'，可能混淆后的名称已改变。");
                return;
            }

            // 5. 设置新值 (例如都设置为 4096 MB)
            var newHsValue = 512;
            var newHtValue = 2147483647;

            fieldHs.SetValue(targetInstance, newHsValue);
            fieldHt.SetValue(targetInstance, newHtValue);

            PluginLog.Info("Game", $"成功修改内存设置：MinMemoryLimit={newHsValue}, MaxMemoryLimit={newHtValue}");
        }
        catch (Exception ex)
        {
            PluginLog.Error("Game", $"发生异常：{ex.Message}");
            PluginLog.Error("Game", ex.StackTrace);
        }
    }

    [HookMethod("WPFLauncher.ViewModel.SysSetting.ho", "r")]
    public static int GetPerfectMemory()
    {
        return MemoryUtils.GetPerfectMemory();
    }

    [HookMethod(TargetConst.JavaProcess, TargetConst.JavaMaxMemorySet)]
    private int JavaMaxMemory()
    {
        return azf<axi>.Instance.User.GameMemorySize;
    }
}