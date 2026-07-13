using Mcl.Core.Dotnetdetour.Tools;

namespace Mcl.Core.Dotnetdetour.HookList;

public class DirectoryEvent : IMethodHook
{
    [OriginalMethod]
    public static void Delete(string path, bool recursive)
    {
        
    }
    
    [HookMethod("Pri.LongPath.Directory", "Delete", "Delete")]
    public static void DeleteHook(string path, bool recursive)
    {
        Logger.Info("Deleting: " + path);
        if ((path.EndsWith("\\resourcepacks") || path.EndsWith("/resourcepacks")) && WpfConfig.KeepOffDeleteLastResourcepacks)
        {
            Logger.Info("阻止网易删除resourcepacks文件夹");
            return;
        }

        if ((path.EndsWith("\\config") || path.EndsWith("/config")) && WpfConfig.KeepOffDeleteLastConfig)
        {
            Logger.Info("阻止网易删除config文件夹");
            return;
        }

        if ((path.EndsWith("\\shaderpacks") || path.EndsWith("/shaderpacks")) && WpfConfig.KeepOffDeleteLastShaderPacks)
        {
            Logger.Info("阻止网易删除shaderpacks文件夹");
            return;
        }
        Delete(path, recursive);
    }
}