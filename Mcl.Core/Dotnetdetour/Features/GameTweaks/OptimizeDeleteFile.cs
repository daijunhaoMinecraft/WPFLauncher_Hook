using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Manager;
using WPFLauncher.Util;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

public class OptimizeDeleteFile : IMethodHook
{
    [HookMethod("WPFLauncher.Model.Game.ale", "a", "DeleteGame")]
    private void DeleteGameHook(object aleInstance)
    {
        WpfConfig.DefaultLogger.Info("触发 DeleteGameHook，开始解析目标路径...");

        // 1. 获取 aleInstance 中的私有字段 'i' (类型为 alp)
        var iField = aleInstance.GetType().GetField("i", BindingFlags.NonPublic | BindingFlags.Instance);
        if (iField == null)
        {
            WpfConfig.DefaultLogger.Error("反射获取字段 'i' 失败，Hook 提前退出。");
            return;
        }

        var iInstance = iField.GetValue(aleInstance);

        // 2. 获取 alp 实例中的 'ExeDirName' 属性
        var exeDirNameProp = iInstance.GetType().GetProperty("ExeDirName", BindingFlags.Public | BindingFlags.Instance);
        if (exeDirNameProp == null)
        {
            WpfConfig.DefaultLogger.Error("反射获取属性 'ExeDirName' 失败，Hook 提前退出。");
            return;
        }

        var exeDirName = (string)exeDirNameProp.GetValue(iInstance);

        // 3. 通过反射获取 aov 的静态字段 c
        var aovCField = typeof(aov).GetField("c", BindingFlags.Public | BindingFlags.Static);
        if (aovCField == null)
        {
            WpfConfig.DefaultLogger.Error("反射获取静态字段 'aov.c' 失败，Hook 提前退出。");
            return;
        }

        var basePath = (string)aovCField.GetValue(null);

        // 4. 拼接最终的目标路径
        var targetPath = basePath + "Cpp\\" + exeDirName;
        WpfConfig.DefaultLogger.Info($"成功解析目标基岩版路径: {targetPath}");

        var res = uz.q("检测到版本更新, 请选择你基岩版本体的操作", "", "重命名", "删除");

        if (res == MessageBoxResult.OK)
        {
            WpfConfig.DefaultLogger.Info("用户选择 [重命名] 操作。");
            if (Directory.Exists(targetPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var newPath = targetPath + "_" + timestamp;
                try
                {
                    Directory.Move(targetPath, newPath);
                    WpfConfig.DefaultLogger.Info($"目录已成功重命名为: {newPath}");
                }
                catch (Exception ex)
                {
                    WpfConfig.DefaultLogger.Error($"重命名失败，文件可能被占用: {ex.Message}");
                }
            }
            else
            {
                WpfConfig.DefaultLogger.Warn("目标目录不存在，无需重命名。");
            }
        }
        else if (res == MessageBoxResult.No)
        {
            WpfConfig.DefaultLogger.Info("用户选择 [删除] 操作，准备执行平滑清理策略...");
            if (Directory.Exists(targetPath))
                SmoothDeleteDirectory(targetPath);
            else
                WpfConfig.DefaultLogger.Warn("目标目录不存在，无需清理。");
        }
    }

    /// <summary>
    ///     针对机械硬盘优化的平滑删除方法
    /// </summary>
    private void SmoothDeleteDirectory(string targetPath)
    {
        var tempPath = targetPath + "_deleting_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        try
        {
            Directory.Move(targetPath, tempPath);
            WpfConfig.DefaultLogger.Info($"瞬间释放原路径成功，已转移至: {tempPath}");
        }
        catch (Exception ex)
        {
            tempPath = targetPath;
            WpfConfig.DefaultLogger.Warn($"瞬间转移目录失败，将降级在原目录操作。原因: {ex.Message}");
        }

        WpfConfig.DefaultLogger.Info("已开启后台低优先级线程进行 I/O 节流清理...");

        Task.Run(() =>
        {
            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                var deletedFilesCount = 0;

                var files = Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        deletedFilesCount++;
                        Thread.Sleep(2);
                    }
                    catch
                    {
                    }

                var dirs = Directory.GetDirectories(tempPath, "*", SearchOption.AllDirectories);
                Array.Sort(dirs, (a, b) => b.Length.CompareTo(a.Length));

                foreach (var dir in dirs)
                    try
                    {
                        Directory.Delete(dir, false);
                    }
                    catch
                    {
                    }

                if (Directory.Exists(tempPath)) Directory.Delete(tempPath, false);

                WpfConfig.DefaultLogger.Info($"后台清理完成！共节流删除文件数: {deletedFilesCount}");
            }
            catch (Exception ex)
            {
                WpfConfig.DefaultLogger.Error($"后台清理线程发生未捕获异常: {ex.Message}");
            }
        });
    }
}