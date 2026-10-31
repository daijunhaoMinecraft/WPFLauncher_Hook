using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Tools;
using Microsoft.VisualBasic.Logging;
using Newtonsoft.Json.Linq;
using NLog;

using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
namespace Mcl.Core.Dotnetdetour.Features.Authentication.Core;

public class RebuildX19Crypto : IMethodHook
{
    // 重构 X19 相关算法
    public const string ClassName = "WPFLauncher.Util.ss";
    
    [HookMethod(ClassName, "b")]
    public static byte[] HttpEncrypt(string path, string body, out string key)
    {
        // HttpEncrypt(path, body, out key);
        var result = X19Crypt.HttpEncrypt(Encoding.UTF8.GetBytes(body));
        var x19Key = X19Crypt.PickKey(result[result.Length - 1]);
        key = Encoding.UTF8.GetString(x19Key);
        if (WpfConfig.EnableVerboseLogging && WpfConfig.LogSensitiveAccountDetails)
            PluginLog.Debug("Auth", 
                $"HttpEncrypt: Path: {path} Body: {body}, EncryptBytesCount: {result.Length}, EncryptKey: {key}");
        return result;
    }

    [HookMethod(ClassName, "c")]
    public static string HttpDecrypt(byte[] encryptBody, out string key)
    {
        var result = X19Crypt.DecryptX19Body(encryptBody);
        var x19Key = X19Crypt.PickKey(encryptBody[encryptBody.Length - 1]);
        key = Encoding.UTF8.GetString(x19Key);
        if (WpfConfig.EnableVerboseLogging && WpfConfig.LogSensitiveAccountDetails)
            PluginLog.Debug("Auth", 
                $"HttpDecrypt: EncryptBodyCount: {encryptBody.Length}, DecryptBody: {result}, EncryptKey: {key}");
        return result;
    }

    [OriginalMethod]
    public static string ParseResponseLogin(byte[] encryptBody, out string key)
    {
        key = "";
        return "";
    }


    [HookMethod(ClassName, "d", "ParseResponseLogin")]
    public static string ParseResponseLoginHook(byte[] encryptBody, out string key)
    {
        var result = ParseResponseLogin(encryptBody, out key);
        if (WpfConfig.EnableVerboseLogging && WpfConfig.LogSensitiveAccountDetails)
            PluginLog.Debug("Auth", 
                $"ParseResponseLogin: EncryptBodyCount: {encryptBody.Length}, DecryptBody: {result}, EncryptKey: {key}");
        return result;
    }

    [HookMethod(ClassName, "e")]
    public static string ComputeDynamicToken(string path, string body)
    {
        var result = X19Crypt.ComputeDynamicToken(path, body);
        if (WpfConfig.EnableVerboseLogging && WpfConfig.LogSensitiveAccountDetails)
            PluginLog.Debug("Auth", 
                $"DynamicToken: Body: {body}, Path: {path}, UserToken: {result}, UserId: {X19Crypt.UserId}");
    
        return result;
    }

    [HookMethod(ClassName, "f")]
    public static string GetH5Token()
    {
        var result = X19Crypt.GetH5Token();
        if (WpfConfig.EnableVerboseLogging && WpfConfig.LogSensitiveAccountDetails) PluginLog.Debug("Auth", $"GetH5Token: Token: {X19Crypt.Token}, H5Token: {result}");

        return result;
    }

    [OriginalMethod]
    public static int UnzipModJsonOriginal(string path) => 0;

    [OriginalMethod]
    public static void GetItemInfo(string itemId, bool unk)
    {
        
    }
    
    [HookMethod(ClassName, "k")]
    public static void GetItemInfoHook(string itemId, bool unk)
    {
        PluginLog.Info("Unzip", $"GetItemInfo, itemId: {itemId}, unkBool: {unk}");
        GetItemInfo(itemId, unk);
    }
    
    [HookMethod(ClassName, "l")]
    public static int UnzipModJson(string path)
    {
        PluginLog.Info("Unzip", $"UnzipMod: {path}");
    
        // try
        // {
        //     if (!string.IsNullOrEmpty(path))
        //     {
        //         string[] suffixes = { ".mczip", ".mergedmcs" };
        //         string newPath = null;
        //
        //         foreach (var suffix in suffixes)
        //         {
        //             if (path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        //             {
        //                 newPath = path.Substring(0, path.Length - suffix.Length);
        //                 break;
        //             }
        //         }
        //
        //         if (newPath != null && !string.Equals(path, newPath, StringComparison.OrdinalIgnoreCase))
        //         {
        //             if (!File.Exists(path))
        //             {
        //                 PluginLog.Info("Unzip", $"文件不存在，跳过重命名: {path}");
        //             }
        //             else
        //             {
        //                 string dir = Path.GetDirectoryName(newPath);
        //                 if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        //                 {
        //                     Directory.CreateDirectory(dir);
        //                 }
        //
        //                 if (File.Exists(newPath))
        //                 {
        //                     PluginLog.Info("Unzip", $"目标已存在，删除源文件: {path} -> {newPath}");
        //                     File.Delete(path);
        //                 }
        //                 else
        //                 {
        //                     File.Move(path, newPath);
        //                     PluginLog.Info("Unzip", $"重命名: {path} -> {newPath}");
        //                 }
        //             }
        //         }
        //     }
        // }
        // catch (Exception ex)
        // {
        //     PluginLog.Info("Unzip", $"去除后缀/重命名失败: {path}, {ex}");
        // }
        // return 0;
        return UnzipModJsonOriginal(path);
    }
}