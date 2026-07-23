using System.Reflection;
using System.Threading;

namespace Mcl.Core.Utils;

// Token: 0x02000002 RID: 2
public class AppMutexHelper
{
    // Token: 0x04000001 RID: 1
    public static Mutex AppMutex;

    // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
    public static bool CheckAppMutex()
    {
        var name = Assembly.GetEntryAssembly().GetName().Name;
        bool flag;
        AppMutex = new Mutex(true, name, out flag);
        return flag;
    }

    // Token: 0x06000002 RID: 2 RVA: 0x00002084 File Offset: 0x00000284
    public static bool CheckAppMutex(string appId)
    {
        //MethodHook.Install(null);
        bool flag;
        AppMutex = new Mutex(true, appId, out flag);
        return true;
    }
}