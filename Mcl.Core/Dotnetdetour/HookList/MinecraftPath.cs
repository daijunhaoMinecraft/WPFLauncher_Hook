using System;
using System.IO;
using Mcl.Core.Dotnetdetour.Tools;
using Mcl.Core.Utils;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.HookList
{
    public class MinecraftPath : IMethodHook
    {
        [OriginalMethod]
        private static string ChangeMinecraftPath()
        {
            return "";
        }
        
        [HookMethod("WPFLauncher.Util.tb", "c", "ChangeMinecraftPath")]
        public static string c()
        {
            string MinecraftPath = ChangeMinecraftPath();
            string NowMinecraftPath = Path.Combine(new string[] { tb.n, "Game", ".minecraft" });
            if (WpfConfig.IsDebug)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[MinecraftPath]Minecraft路径: " + MinecraftPath);
                Console.WriteLine("[MinecraftPath]现在Mincraft路径: " + NowMinecraftPath);
            }
            return NowMinecraftPath;
        }
        
        [HookMethod("WPFLauncher.Util.we", "ac", null)]
        public static bool CheckPermission(string directory)
        {
            WpfConfig.DefaultLogger.Info($"开始检查目录状态: \"{directory}\"");

            // 1. 检查路径字符串是否为空
            if (string.IsNullOrWhiteSpace(directory))
            {
                WpfConfig.DefaultLogger.Error("检查失败: 传入的路径为空或全是空格。");
                return false;
            }

            try
            {
                // 2. 检查路径是否正常（格式是否合法）
                string fullPath = Path.GetFullPath(directory);
                WpfConfig.DefaultLogger.Info($"路径格式验证通过，绝对路径为: {fullPath}");

                // 3. 检查目录是否存在
                if (!Directory.Exists(fullPath))
                {
                    WpfConfig.DefaultLogger.Warn($"目录不存在: {fullPath}");
                    return false; 
                }
                WpfConfig.DefaultLogger.Info("目录存在，准备检查访问权限...");

                // 4. 检查是否可访问 (尝试读取目录信息)
                Directory.GetDirectories(fullPath);
                WpfConfig.DefaultLogger.Info("目录读取权限正常，准备检查写入权限...");

                // 5. 检查是否可写入 (尝试创建并删除临时文件)
                string tempFilePath = Path.Combine(fullPath, Guid.NewGuid().ToString("N") + ".tmp");
                using (FileStream fs = new FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.DeleteOnClose))
                {
                    fs.WriteByte(0);
                }
                
                // 全通过
                WpfConfig.DefaultLogger.Info($"检查完成！该目录状态正常，允许访问及写入。");
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                WpfConfig.DefaultLogger.Error($"权限不足，拒绝访问 (请检查是否需要管理员权限): {ex.Message}");
                return false; 
            }
            catch (PathTooLongException)
            {
                WpfConfig.DefaultLogger.Error("路径字符串太长，系统无法处理。");
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                WpfConfig.DefaultLogger.Error("找不到该驱动器或路径中的某一层文件夹。");
                return false;
            }
            catch (NotSupportedException)
            {
                WpfConfig.DefaultLogger.Error("路径格式不受支持 (可能包含了无效的驱动器盘符)。");
                return false;
            }
            catch (IOException ex)
            {
                WpfConfig.DefaultLogger.Error($"发生 IO 错误 (可能是磁盘已满，或网络共享断开): {ex.Message}");
                return false;
            }
            catch (ArgumentException)
            {
                WpfConfig.DefaultLogger.Error("路径包含非法字符 (例如 < > | 等)。");
                return false;
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"发生未知异常: {ex.Message}");
                return false;
            }
        }
        
        [HookMethod("WPFLauncher.Util.tb", "i", null)]
        private static string InitializeBedrockPath()
        {
            string result = "";
            string regGetPath = RegistryHelper.GetValue("MinecraftBENeteasePath");
            string userSelectPath = WpfConfig.BedrockPath;
            bool checkResultUserSelectPath = CheckPermission(userSelectPath);
            bool checkResultRegGetPath = CheckPermission(regGetPath);
            result = userSelectPath;
            if (!checkResultUserSelectPath && !checkResultRegGetPath)
            {
                WpfConfig.DefaultLogger.Error($"MinecraftBENeteasePath not valid permission: {regGetPath} | {userSelectPath}");
                result = Path.Combine(tb.n, "MinecraftBENeteasePath");
                RegistryHelper.SetValue("MinecraftBENeteasePath", result);
            }
            if (!checkResultUserSelectPath)
            {
                result = regGetPath;
            }

            WpfConfig.BedrockPath = result;
            return result;
        }
    }
}