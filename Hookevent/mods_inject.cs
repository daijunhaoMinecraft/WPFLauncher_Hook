using System;
using WPFLauncher.Manager;
using WPFLauncher.ViewModel.Launcher;
using Path = System.Windows.Shapes.Path;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using WPFLauncher.Util;
namespace DotNetTranstor.Hookevent
{
    public class mods_inject : IMethodHook
    {
        [OriginalMethod]
        private aqj StartJava_Original(string oby, EventHandler obz, JavaType oca, string ocb, bool occ = true)
        {
            return null;
        }
        [HookMethod("WPFLauncher.Manager.Game.Launcher.auo", "o", "StartJava_Original")]
        private aqj StartJava(string oby, EventHandler obz, JavaType oca, string ocb, bool occ = true)
        {
            if (Path_Bool.EnableModsInject)
            {
                string modsInjectPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "ModsInject");
                string minecraftModsPath = System.IO.Path.Combine(su.z, "mods");

                // 如果目标路径不存在，则创建它
                if (!System.IO.Directory.Exists(minecraftModsPath))
                {
                    System.IO.Directory.CreateDirectory(minecraftModsPath);
                }

                // 获取ModsInject文件夹中的所有.jar文件
                string[] jarFiles = System.IO.Directory.GetFiles(modsInjectPath, "*.jar");
                
                MD5 md5 = MD5.Create();
                
                foreach (string jarFile in jarFiles)
                {
                    string fileName = System.IO.Path.GetFileName(jarFile);
                    // 复制文件到目标路径
                    
                    FileInfo file = new FileInfo(jarFile);
                    string filename = file.Name;
                    string newfilename = "😅" + jsmhToolChest.Libraries.String.ByteArrayToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(filename))) + "😅.jar";
                    string destinationPath = System.IO.Path.Combine(minecraftModsPath, newfilename);

                    System.IO.File.Copy(jarFile, destinationPath, true);
                    Console.WriteLine($"[INFO]成功复制模组: {fileName}(修改后的文件名称:{newfilename}) 到 {minecraftModsPath}");
                }
                System.Diagnostics.Process.Start("explorer.exe", minecraftModsPath);
                us.n("已为您打开模组文件夹,确认模组无误后再点击确定即可开始游戏", "");
            }
            return StartJava_Original(oby, obz, oca, ocb, occ);
        }
    }
}