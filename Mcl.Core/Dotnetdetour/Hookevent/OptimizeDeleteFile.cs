using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WPFLauncher.Util;
using Logger = Mcl.Core.Dotnetdetour.Tools.Logger;

namespace Mcl.Core.Dotnetdetour.Hookevent;

public class OptimizeDeleteFile : IMethodHook
{
    [HookMethod("WPFLauncher.Model.Game.ale", "a", "DeleteGame")]
    private void DeleteGameHook(object aleInstance)
    {
        Logger.Info("触发 DeleteGameHook，开始解析目标路径...");

        // 1. 获取 aleInstance 中的私有字段 'i' (类型为 alp)
        FieldInfo iField = aleInstance.GetType().GetField("i", BindingFlags.NonPublic | BindingFlags.Instance);
        if (iField == null) 
        {
            Logger.Error("反射获取字段 'i' 失败，Hook 提前退出。");
            return;
        }
        object iInstance = iField.GetValue(aleInstance);

        // 2. 获取 alp 实例中的 'ExeDirName' 属性
        PropertyInfo exeDirNameProp = iInstance.GetType().GetProperty("ExeDirName", BindingFlags.Public | BindingFlags.Instance);
        if (exeDirNameProp == null) 
        {
            Logger.Error("反射获取属性 'ExeDirName' 失败，Hook 提前退出。");
            return;
        }
        string exeDirName = (string)exeDirNameProp.GetValue(iInstance);

        // 3. 通过反射获取 aov 的静态字段 c
        FieldInfo aovCField = typeof(WPFLauncher.Manager.aov).GetField("c", BindingFlags.Public | BindingFlags.Static);
        if (aovCField == null) 
        {
            Logger.Error("反射获取静态字段 'aov.c' 失败，Hook 提前退出。");
            return;
        }
        string basePath = (string)aovCField.GetValue(null);

        // 4. 拼接最终的目标路径
        string targetPath = basePath + "Cpp\\" + exeDirName;
        Logger.Success($"成功解析目标基岩版路径: {targetPath}");

        MessageBoxResult res = uz.q("检测到版本更新, 请选择你基岩版本体的操作", "", "重命名", "删除", "");
        
        if (res == MessageBoxResult.OK)
        {
            Logger.Info("用户选择 [重命名] 操作。");
            if (Directory.Exists(targetPath))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string newPath = targetPath + "_" + timestamp;
                try
                {
                    Directory.Move(targetPath, newPath);
                    Logger.Success($"目录已成功重命名为: {newPath}");
                }
                catch (Exception ex)
                {
                    Logger.Error($"重命名失败，文件可能被占用: {ex.Message}");
                }
            }
            else
            {
                Logger.Warn("目标目录不存在，无需重命名。");
            }
        }
        else if (res == MessageBoxResult.No)
        {
            Logger.Info("用户选择 [删除] 操作，准备执行平滑清理策略...");
            if (Directory.Exists(targetPath))
            {
                SmoothDeleteDirectory(targetPath);
            }
            else
            {
                Logger.Warn("目标目录不存在，无需清理。");
            }
        }
    }

    /// <summary>
    /// 针对机械硬盘优化的平滑删除方法
    /// </summary>
    private void SmoothDeleteDirectory(string targetPath)
    {
        string tempPath = targetPath + "_deleting_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        try
        {
            Directory.Move(targetPath, tempPath);
            Logger.Success($"瞬间释放原路径成功，已转移至: {tempPath}");
        }
        catch (Exception ex)
        {
            tempPath = targetPath;
            Logger.Warn($"瞬间转移目录失败，将降级在原目录操作。原因: {ex.Message}");
        }

        Logger.Info("已开启后台低优先级线程进行 I/O 节流清理...");

        Task.Run(() =>
        {
            try
            {
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                int deletedFilesCount = 0;

                string[] files = Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal); 
                        File.Delete(file);
                        deletedFilesCount++;
                        Thread.Sleep(2); 
                    }
                    catch {}
                }

                string[] dirs = Directory.GetDirectories(tempPath, "*", SearchOption.AllDirectories);
                Array.Sort(dirs, (a, b) => b.Length.CompareTo(a.Length)); 
                
                foreach (string dir in dirs)
                {
                    try { Directory.Delete(dir, false); } catch {}
                }

                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, false);
                }

                Logger.Success($"后台清理完成！共节流删除文件数: {deletedFilesCount}");
            }
            catch (Exception ex)
            {
                Logger.Error($"后台清理线程发生未捕获异常: {ex.Message}");
            }
        });
    }
}