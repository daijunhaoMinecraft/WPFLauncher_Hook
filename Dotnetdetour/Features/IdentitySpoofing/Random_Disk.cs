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
        
        [HookMethod("WPFLauncher.Manager.aqm", null, null)]
        public static string a(object nbv)
        {
            return "{}";
        }
        
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