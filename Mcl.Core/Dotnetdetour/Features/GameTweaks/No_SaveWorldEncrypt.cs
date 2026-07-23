using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.Features.GameTweaks;

//去除网易存档加密功能
internal class No_SaveWorldEncrypt : IMethodHook
{
    [CompilerGenerated]
    [HookMethod("MCStudio.Utils.cd", "b")]
    public static string EncryptFolder(string path)
    {
        WpfConfig.DefaultLogger.Info($"发现网易正在加密存档已被制止,存档路径:{path}");
        return "";
    }

    [CompilerGenerated]
    [HookMethod("MCStudio.Utils.cd", "d")]
    private static string EncryptSingleFile(string path)
    {
        WpfConfig.DefaultLogger.Info($"[INFO]发现网易正在加密存档已被制止,加密文件路径:{path}");
        return "";
    }
}