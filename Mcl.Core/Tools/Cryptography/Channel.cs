using System.IO;
using Mcl.Core.Utils;
using Microsoft.Win32;

namespace Mcl.Core.Tools.Cryptography;

public class Channel
{
    public static string ChannelGet()
    {
        if (File.Exists("4399pc.data"))
        {
            return "4399pc";
        }

        if (File.Exists("native_a50_cn.data"))
        {
            return "a50_sdk_cn";
        }

        return "netease";
    }

    public static void InitializeRegistry()
    {
        string channel = ChannelGet();
        string registrySubKey = "SOFTWARE\\Netease\\";
        switch (channel)
        {
            case "4399pc":
                registrySubKey += "PC4399_";
                break;
            case "a50_sdk_cn":
                registrySubKey += "A50SdkCn_";
                break;
        }
        registrySubKey += "MCLauncher";
        RegistryHelper.InitRegistryKey(registrySubKey);
    }
}