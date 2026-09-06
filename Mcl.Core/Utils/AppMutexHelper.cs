using System.Reflection;
using System.Threading;

namespace Mcl.Core.Utils;

public class AppMutexHelper
{
    public static Mutex AppMutex;

    public static bool CheckAppMutex()
    {
        var name = Assembly.GetEntryAssembly().GetName().Name;
        bool flag;
        AppMutex = new Mutex(true, name, out flag);
        return flag;
    }

    public static bool CheckAppMutex(string appId)
    {
        //MethodHook.Install(null);
        bool flag;
        AppMutex = new Mutex(true, appId, out flag);
        return true;
    }
}