using Azure;
using DotNetTranstor;

namespace MicrosoftTranslator.DotNetTranstor.Hookevent
{
    public class Random_Disk : IMethodHook
    {
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
                    text = Tool.getDiskCode();
                }
                Tool.PrintGreen("虚拟机器码：" + text);
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
                    text = Tool.getCPUID();
                }
                text += kxr;
                if (text.Length > 0x18)
                {
                    text.Substring(0, 0x18);
                }
                Tool.PrintGreen("UUID：" + text);
            }
            catch
            {
                text = null;
            }
            return text;
        }
    }
}