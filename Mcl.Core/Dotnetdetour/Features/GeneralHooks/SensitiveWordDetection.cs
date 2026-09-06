using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

/// <summary>
///     网易敏感词检测拦截类
///     整合了所有敏感词检测相关的拦截功能
/// </summary>
internal class SensitiveWordDetection : IMethodHook
{
    public const string ClassName = "WPFLauncher.cn";
    #region 初始化敏感词功能拦截

    [OriginalMethod]
    public static void No_Sensitive_word_Init()
    {
    }

    [CompilerGenerated]
    [HookMethod(ClassName, "e", "No_Sensitive_word_Init")]
    public static void InitHookA()
    {
        if (WpfConfig.EnableVerboseLogging) PluginLog.Warn("Hook", "[INFO]发现网易正在初始化敏感词功能已被制止");
    }

    #endregion

    #region 敏感词检测拦截 - 字符串返回类型

    [OriginalMethod]
    public static string No_Sensitive_word_String(string content)
    {
        return content;
    }

    [CompilerGenerated]
    [HookMethod(ClassName, "g", "No_Sensitive_word_String")]
    public static string StringHookC(string content)
    {
        if (WpfConfig.EnableVerboseLogging) PluginLog.Warn("Hook", $"[INFO]发现网易检测名称敏感词已被制止, 检测的内容为:{content}");
        return content;
    }

    [CompilerGenerated]
    [HookMethod(ClassName, "i", "No_Sensitive_word_String")]
    public static string StringHookE(string content)
    {
        if (WpfConfig.EnableVerboseLogging) PluginLog.Warn("Hook", $"[INFO]发现网易检测文本敏感词已被制止, 检测的内容为:{content}");
        return content;
    }

    #endregion

    #region 敏感词检测拦截 - 布尔返回类型

    [OriginalMethod]
    public static bool No_Sensitive_word_Bool(string content)
    {
        return true;
    }

    [CompilerGenerated]
    [HookMethod(ClassName, "h", "No_Sensitive_word_Bool")]
    public static bool BoolHookD(string content)
    {
        if (WpfConfig.EnableVerboseLogging) PluginLog.Warn("Hook", $"[INFO]发现网易检测名称敏感词已被制止, 检测的内容为:{content}");
        return true;
    }

    [CompilerGenerated]
    [HookMethod(ClassName, "j", "No_Sensitive_word_Bool")]
    public static bool BoolHookF(string content)
    {
        if (WpfConfig.EnableVerboseLogging) PluginLog.Warn("Hook", $"[INFO]发现网易检测文本敏感词已被制止, 检测的内容为:{content}");
        return true;
    }

    #endregion
}