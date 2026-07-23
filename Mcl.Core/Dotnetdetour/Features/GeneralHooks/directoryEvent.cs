using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

public class DirectoryEvent : IMethodHook
{
    [OriginalMethod]
    public static void Delete(string path, bool recursive)
    {
    }

    [HookMethod("Pri.LongPath.Directory", "Delete", "Delete")]
    public static void DeleteHook(string path, bool recursive)
    {
        WpfConfig.DefaultLogger.Info("Deleting: " + path);
        if ((path.EndsWith("\\resourcepacks") || path.EndsWith("/resourcepacks")) &&
            WpfConfig.KeepOffDeleteLastResourcepacks)
        {
            WpfConfig.DefaultLogger.Info("阻止网易删除resourcepacks文件夹");
            return;
        }

        if ((path.EndsWith("\\config") || path.EndsWith("/config")) && WpfConfig.KeepOffDeleteLastConfig)
        {
            WpfConfig.DefaultLogger.Info("阻止网易删除config文件夹");
            return;
        }

        if ((path.EndsWith("\\shaderpacks") || path.EndsWith("/shaderpacks")) && WpfConfig.KeepOffDeleteLastShaderPacks)
        {
            WpfConfig.DefaultLogger.Info("阻止网易删除shaderpacks文件夹");
            return;
        }

        Delete(path, recursive);
    }
}