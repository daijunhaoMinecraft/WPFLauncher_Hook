using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using WPFLauncher.Manager;
using WPFLauncher.Util;
using WPFLauncher.ViewModel.Launcher;

namespace Mcl.Core.Dotnetdetour.Hookevent
{
    public class ModsInject : IMethodHook
    {
        public static string ByteArrayToHexString(byte[] byteArray)
        {
            StringBuilder hex = new StringBuilder(byteArray.Length * 2);
            foreach (byte b in byteArray)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        [OriginalMethod]
        private aqq StartJava_Original(string oby, EventHandler obz, JavaType oca, string ocb, bool occ = true)
        {
            return null;
        }
        [HookMethod("WPFLauncher.Manager.Game.Launcher.auw", "o", "StartJava_Original")]
        private aqq StartJava(string oby, EventHandler obz, JavaType oca, string ocb, bool occ = true)
        {
            if (Path_Bool.EnableModsInject)
            {
                string modsInjectPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "ModsInject");
                string minecraftModsPath = System.IO.Path.Combine(tb.z, "mods");

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
                    string newfilename = "😅" + ByteArrayToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(filename))) + "😅.jar";
                    string destinationPath = System.IO.Path.Combine(minecraftModsPath, newfilename);

                    System.IO.File.Copy(jarFile, destinationPath, true);
                    Console.WriteLine($"[INFO]成功复制模组: {fileName}(修改后的文件名称:{newfilename}) 到 {minecraftModsPath}");
                }
                System.Diagnostics.Process.Start("explorer.exe", minecraftModsPath);
                //uz.n("已为您打开模组文件夹,确认模组无误后再点击确定即可开始游戏", "");
            }
            return StartJava_Original(oby, obz, oca, ocb, occ);
        }
    }
}