using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using System;
using System.IO;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Utils;
using WPFLauncher.Model;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

public class MinecraftPath : IMethodHook
{
    [OriginalMethod]
    private static string ChangeMinecraftPath()
    {
        return "";
    }

    [HookMethod("WPFLauncher.Util.tb", "c", "ChangeMinecraftPath")]
    public static string GetMinecraftPath(GameVersion version = GameVersion.NONE)
    {
        var MinecraftPath = ChangeMinecraftPath();
        var NowMinecraftPath = Path.Combine(new[] { tb.n, "Game", ".minecraft" });
        return NowMinecraftPath;
    }

    [HookMethod("WPFLauncher.Util.we", "ac", null)]
    public static bool CheckPermission(string directory)
    {
        PluginLog.Info("Game", $"开始检查目录状态: \"{directory}\"");

        // 1. 检查路径字符串是否为空
        if (string.IsNullOrWhiteSpace(directory))
        {
            PluginLog.Error("Game", "检查失败: 传入的路径为空或全是空格。");
            return false;
        }

        try
        {
            // 2. 检查路径是否正常（格式是否合法）
            var fullPath = Path.GetFullPath(directory);
            PluginLog.Info("Game", $"路径格式验证通过，绝对路径为: {fullPath}");

            // 3. 检查目录是否存在
            if (!Directory.Exists(fullPath))
            {
                PluginLog.Warn("Game", $"目录不存在: {fullPath}");
                return false;
            }

            PluginLog.Info("Game", "目录存在，准备检查访问权限...");

            // 4. 检查是否可访问 (尝试读取目录信息)
            Directory.GetDirectories(fullPath);
            PluginLog.Info("Game", "目录读取权限正常，准备检查写入权限...");

            // 5. 检查是否可写入 (尝试创建并删除临时文件)
            var tempFilePath = Path.Combine(fullPath, Guid.NewGuid().ToString("N") + ".tmp");
            using (var fs = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
                       FileOptions.DeleteOnClose))
            {
                fs.WriteByte(0);
            }

            // 全通过
            PluginLog.Info("Game", "检查完成！该目录状态正常，允许访问及写入。");
            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            PluginLog.Error("Game", $"权限不足，拒绝访问 (请检查是否需要管理员权限): {ex.Message}");
            return false;
        }
        catch (PathTooLongException)
        {
            PluginLog.Error("Game", "路径字符串太长，系统无法处理。");
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            PluginLog.Error("Game", "找不到该驱动器或路径中的某一层文件夹。");
            return false;
        }
        catch (NotSupportedException)
        {
            PluginLog.Error("Game", "路径格式不受支持 (可能包含了无效的驱动器盘符)。");
            return false;
        }
        catch (IOException ex)
        {
            PluginLog.Error("Game", $"发生 IO 错误 (可能是磁盘已满，或网络共享断开): {ex.Message}");
            return false;
        }
        catch (ArgumentException)
        {
            PluginLog.Error("Game", "路径包含非法字符 (例如 < > | 等)。");
            return false;
        }
        catch (Exception ex)
        {
            PluginLog.Error("Game", $"发生未知异常: {ex.Message}");
            return false;
        }
    }

    [HookMethod("WPFLauncher.Util.tb", "i")]
    private static string InitializeBedrockPath()
    {
        var result = "";
        var regGetPath = RegistryHelper.GetValue("MinecraftBENeteasePath");
        var userSelectPath = WpfConfig.BedrockDirectory;
        var checkResultUserSelectPath = CheckPermission(userSelectPath);
        var checkResultRegGetPath = CheckPermission(regGetPath);
        result = userSelectPath;
        if (!checkResultUserSelectPath && !checkResultRegGetPath)
        {
            PluginLog.Error("Game", 
                $"MinecraftBENeteasePath not valid permission: {regGetPath} | {userSelectPath}");
            result = Path.Combine(tb.n, "MinecraftBENeteasePath");
            RegistryHelper.SetValue("MinecraftBENeteasePath", result);
        }

        if (!checkResultUserSelectPath) result = regGetPath;

        WpfConfig.BedrockDirectory = result;
        return result;
    }
}