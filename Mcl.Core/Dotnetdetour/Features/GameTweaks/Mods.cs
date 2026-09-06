using System;
using System.IO;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

internal class Mods : IMethodHook
{
    public Mods()
    {
        if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", "Mods 构造函数被调用");
    }

    [HookMethod("System.IO.Directory")]
    public static string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        if (WpfConfig.EnableVerboseLogging)
            PluginLog.Debug("Game", 
                $"GetFiles 被调用，路径: {path}, 搜索模式: {searchPattern}, 搜索选项: {searchOption}"); // 输出调试信息

        var array = GetFiles_Original(path, searchPattern, searchOption);
        if (array == null)
        {
            if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", "原始文件数组为空，返回空数组"); // 输出调试信息

            array = new string[0];
        }
        else
        {
            if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", $"原始文件数组长度: {array.Length}"); // 输出调试信息
        }

        for (var i = 0; i < array.Length; i++)
            if (!isGoodMod(array[i]))
            {
                if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", $"文件 {array[i]} 不是有效的模组，设置为空字符串"); // 输出调试信息

                array[i] = "";
            }

        return array;
    }

    [OriginalMethod]
    public static string[] GetFiles_Original(string path, string searchPattern, SearchOption searchOption)
    {
        return null;
    }

    [HookMethod("System.IO.Directory")]
    public static string[] GetFiles(string path)
    {
        if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", $"GetFiles 被调用，路径: {path}"); // 输出调试信息

        var array = GetFiles_Original(path);
        if (array == null)
        {
            if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", "原始文件数组为空，返回空数组"); // 输出调试信息

            array = new string[0];
        }
        else
        {
            if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", $"原始文件数组长度: {array.Length}"); // 输出调试信息
        }

        for (var i = 0; i < array.Length; i++)
            if (!isGoodMod(array[i]))
            {
                if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", $"文件 {array[i]} 不是有效的模组，设置为空字符串"); // 输出调试信息

                array[i] = "";
            }

        return array;
    }

    [OriginalMethod]
    public static string[] GetFiles_Original(string path)
    {
        return null;
    }

    [HookMethod("System.IO.Directory")]
    public static string[] GetFiles(string path, string searchPattern)
    {
        if (WpfConfig.EnableVerboseLogging)
            PluginLog.Debug("Game", $"GetFiles 被调用，路径: {path}, 搜索模式: {searchPattern}"); // 输出调试信息

        var array = GetFiles_Original(path, searchPattern);
        if (array == null)
        {
            if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", "原始文件数组为空，返回空数组"); // 输出调试信息

            array = new string[0];
        }
        else
        {
            if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", $"原始文件数组长度: {array.Length}"); // 输出调试信息
        }

        for (var i = 0; i < array.Length; i++)
            if (!isGoodMod(array[i]))
            {
                if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", $"文件 {array[i]} 不是有效的模组，设置为空字符串"); // 输出调试信息

                array[i] = "";
            }

        return array;
    }

    [OriginalMethod]
    public static string[] GetFiles_Original(string path, string searchPattern)
    {
        return null;
    }

    public static bool isGoodMod(string filePath)
    {
        try
        {
            var isValid = filePath.Contains("\\Game\\.minecraft\\mods\\") && filePath.EndsWith(".jar") &&
                          !filePath.Contains("@");
            if (isValid)
            {
                if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", $"文件 {filePath} 不是有效的模组"); // 输出调试信息

                return false;
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error("Game", $"[ERROR] 检查模组时发生异常: {ex.Message}"); // 输出错误信息

            return false;
        }

        return true;
    }

    [HookMethod("WPFLauncher.Manager.Auth.ats")]
    public bool get_modsChanged()
    {
        if (WpfConfig.EnableVerboseLogging) PluginLog.Debug("Game", "get_modsChanged 被调用"); // 输出调试信息

        return false;
    }

    [OriginalMethod]
    private static void get_modsChanged_Original()
    {
    }
}