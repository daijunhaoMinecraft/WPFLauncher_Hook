using System;
using System.Diagnostics;
using System.Reflection;

namespace Mcl.Core.Utils.Util;

public class FileVersionHelper
{
    public static string GetVersion(string executablePath = null)
    {
        string text2;
        try
        {
            var flag = string.IsNullOrEmpty(executablePath);
            string text;
            if (flag)
            {
                text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            else
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(executablePath);
                text = versionInfo.FileVersion;
            }

            text2 = text;
        }
        catch (Exception)
        {
            text2 = "0.0.0.0";
        }

        return text2;
    }
}