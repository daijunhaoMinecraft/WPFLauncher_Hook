using System;
using Mcl.Core.Dotnetdetour.Tools;

namespace Mcl.Core.Dotnetdetour.HookList
{
    public class RandomDevicesInfo : IMethodHook
    {
        public static string RandomStr(int len, string[] arr = null)
        {
            if (arr == null || arr.Length <= 1)
                arr = new[]
                {
                    "a", "b", "c", "d", "e", "f", "0", "1", "2", "3",
                    "4", "5", "6", "7", "8", "9"
                };
            var text = "";
            for (var i = 0; i < len; i++)
                text += arr[new Random(new Random(Guid.NewGuid().GetHashCode()).Next(0, 0x64)).Next(arr.Length - 1)];
            return text;
        }
        
        // Token: 0x060003F2 RID: 1010 RVA: 0x00003EEC File Offset: 0x000020EC
        [HookMethod("WPFLauncher.Manager.aqm", null, null)]
        public static string a(object nbv)
        {
            return "{}";
        }
        
        // Token: 0x060003F3 RID: 1011 RVA: 0x0000F0F8 File Offset: 0x0000D2F8
        [HookMethod("WPFLauncher.Manager.aqm", null, null)]
        public static string g()
        {
            string text = "";
            string text2;
            try
            {
                if (text == null || text.Length != 8)
                {
                    text = RandomStr(8).ToUpper();
                }
                WpfConfig.DefaultLogger.Info("虚拟机器码: " + text);
                return text;
            }
            catch
            {
                text2 = null;
            }
            return text2;
        }
        // Token: 0x060003F5 RID: 1013 RVA: 0x0000F148 File Offset: 0x0000D348
        [HookMethod("WPFLauncher.Manager.aqm", null, null)]
        public static string e(string kxr)
        {
            string text = "";
            try
            {
                if (text == null || text.Length != 0x10)
                {
                    text = RandomStr(0x10).ToUpper();
                }
                text += kxr;
                if (text.Length > 0x18)
                {
                    text.Substring(0, 0x18);
                }
                WpfConfig.DefaultLogger.Info("UUID: " + text);
            }
            catch
            {
                text = null;
            }
            return text;
        }
    }
}