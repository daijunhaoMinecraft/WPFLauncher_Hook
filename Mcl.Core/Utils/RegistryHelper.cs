using System;
using Microsoft.Win32;

namespace Mcl.Core.Utils;

// Token: 0x02000004 RID: 4
public class RegistryHelper
{
    // Token: 0x04000005 RID: 5
    private static RegistryKey registrykey;

    // Token: 0x06000004 RID: 4 RVA: 0x000020B0 File Offset: 0x000002B0
    public static void InitRegistryKey(string path)
    {
        try
        {
            registrykey = Registry.CurrentUser.CreateSubKey(path);
        }
        catch (Exception ex)
        {
        }
    }

    // Token: 0x06000005 RID: 5 RVA: 0x000020E8 File Offset: 0x000002E8
    public static void SetValue(string key, string value)
    {
        try
        {
            var registryKey = registrykey;
            if (registryKey != null) registryKey.SetValue(key, value);
        }
        catch (Exception ex)
        {
        }
    }

    // Token: 0x06000006 RID: 6 RVA: 0x00002124 File Offset: 0x00000324
    public static string GetValue(string key)
    {
        string text2;
        try
        {
            var registryKey = registrykey;
            string text;
            if (registryKey == null)
            {
                text = null;
            }
            else
            {
                var value = registryKey.GetValue(key);
                text = value != null ? value.ToString() : null;
            }

            text2 = text;
        }
        catch (Exception ex)
        {
            text2 = null;
        }

        return text2;
    }
}