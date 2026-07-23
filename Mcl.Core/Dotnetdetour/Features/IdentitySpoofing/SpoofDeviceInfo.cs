using System;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.Features.IdentitySpoofing;

public class SpoofDeviceInfo : IMethodHook
{
    public const string ClassName = "WPFLauncher.Manager.aqm";
    
    public static string RandomStr(int len, string[] arr = null)
    {
        if (arr == null || arr.Length <= 1)
            arr = new[]
            {
                "a", "b", "c", "d", "e", "f", "0", "1", "2", "3",
                "4", "5", "6", "7", "8", "9"
            };
        var text = "";
        for (var i = 0; i < len; i++)
            text += arr[new Random(new Random(Guid.NewGuid().GetHashCode()).Next(0, 0x64)).Next(arr.Length - 1)];
        return text;
    }

    [HookMethod(ClassName, "g", null)]
    public static string GetCDiskSerialNumber()
    {
        var text = "";
        try
        {
            if (text.Length != 8) text = RandomStr(8).ToUpper();
            WpfConfig.DefaultLogger.Info("虚拟机器码: " + text);
            return text;
        }
        catch
        {
            // ignored
        }

        return null;
    }

    [HookMethod(ClassName, "e", null)]
    public static string GenerateUuid(string suffix)
    {
        const int RandomPartLength = 0x10; // 16
        const int MaxTotalLength = 0x18;   // 24

        string result = "";
        try
        {
            // 如果 result 未初始化或长度不是 16，则生成随机大写字符串
            if (string.IsNullOrEmpty(result) || result.Length != RandomPartLength)
            {
                result = RandomStr(RandomPartLength).ToUpperInvariant();
            }

            result += suffix;

            // 截取前 24 个字符
            if (result.Length > MaxTotalLength)
            {
                result = result.Substring(0, MaxTotalLength);
            }

            WpfConfig.DefaultLogger.Info($"CPUID: {result}");
        }
        catch
        {
            // 记录异常（如有需要可补充日志）
            result = null;
        }

        return result;
    }
}