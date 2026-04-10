using System;
using System.Reflection;
using DotNetTranstor;
using WPFLauncher.Common;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.SQLite;
using WPFLauncher.Util;
using WPFLauncher.View.SysSetting;

namespace Mcl.Core.DotNetTranstor.Hookevent;

// 解决Java版最大内存只能设置到8191问题
public class SolveJavaMemoryLimit : IMethodHook
{
    
    [HookMethod("WPFLauncher.ViewModel.SysSetting.ho", "b", null)]
    public static void ModifyMemorySettings(object targetInstance)
    {
        if (targetInstance == null)
        {
            Console.WriteLine("错误：未找到 SysSetting 实例。");
            return;
        }

        // 1. 获取对象的类型
        Type type = targetInstance.GetType();

        // 确认类型名称是否匹配（可选，用于调试）
        // Console.WriteLine($"Target Type: {type.FullName}");

        // 2. 定义 BindingFlags：非公开 (NonPublic) + 实例 (Instance)
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        try
        {
            // 3. 获取 hs 字段
            FieldInfo fieldHs = type.GetField("hs", flags);
            if (fieldHs == null)
            {
                Console.WriteLine("错误：未找到字段 'hs'，可能混淆后的名称已改变。");
                return;
            }

            // 4. 获取 ht 字段
            FieldInfo fieldHt = type.GetField("ht", flags);
            if (fieldHt == null)
            {
                Console.WriteLine("错误：未找到字段 'ht'，可能混淆后的名称已改变。");
                return;
            }

            // 5. 设置新值 (例如都设置为 4096 MB)
            int newHsValue = 512;
            int newHtValue = 2147483647;

            fieldHs.SetValue(targetInstance, newHsValue);
            fieldHt.SetValue(targetInstance, newHtValue);

            Console.WriteLine($"成功修改内存设置：MinMemoryLimit={newHsValue}, MaxMemoryLimit={newHtValue}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生异常：{ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
    
    [HookMethod("WPFLauncher.ViewModel.SysSetting.ho","r",null)]
    public static int GetPerfectMemory()
    {
        return MemoryUtils.GetPerfectMemory();
    }
    
    [HookMethod("WPFLauncher.Manager.Game.Launcher.auw","g",null)]
    private int JavaMaxMemory()
    {
        return WPFLauncher.Common.azf<WPFLauncher.Manager.Configuration.axi>.Instance.User.GameMemorySize;
    }
}