using System;
using Microsoft.Win32;

namespace Mcl.Core.Utils;

public class RegistryHelper
{
    private static RegistryKey registrykey;

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