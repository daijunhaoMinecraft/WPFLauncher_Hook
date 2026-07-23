using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Manager;
using WPFLauncher.Util;
using WPFLauncher.ViewModel.Launcher;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

public class ModsInject : IMethodHook
{
    public static string ByteArrayToHexString(byte[] byteArray)
    {
        var hex = new StringBuilder(byteArray.Length * 2);
        foreach (var b in byteArray) hex.AppendFormat("{0:x2}", b);
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
        if (WpfConfig.EnableModsInject)
        {
            var modsInjectPath = Path.Combine(Directory.GetCurrentDirectory(), "ModsInject");
            var minecraftModsPath = Path.Combine(tb.z, "mods");

            // 如果目标路径不存在，则创建它
            if (!Directory.Exists(minecraftModsPath)) Directory.CreateDirectory(minecraftModsPath);

            // 获取ModsInject文件夹中的所有.jar文件
            var jarFiles = Directory.GetFiles(modsInjectPath, "*.jar");

            var md5 = MD5.Create();

            foreach (var jarFile in jarFiles)
            {
                var fileName = Path.GetFileName(jarFile);
                // 复制文件到目标路径

                var file = new FileInfo(jarFile);
                var filename = file.Name;
                var newfilename = "😅" + ByteArrayToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(filename))) +
                                  "😅.jar";
                var destinationPath = Path.Combine(minecraftModsPath, newfilename);

                File.Copy(jarFile, destinationPath, true);
                WpfConfig.DefaultLogger.Info($"成功复制模组: {fileName}(修改后的文件名称:{newfilename}) 到 {minecraftModsPath}");
            }
        }

        return StartJava_Original(oby, obz, oca, ocb, occ);
    }
}