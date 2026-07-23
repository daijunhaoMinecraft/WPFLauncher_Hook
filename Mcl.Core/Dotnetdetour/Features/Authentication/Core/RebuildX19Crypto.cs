using System.Text;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Tools;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Core;

public class RebuildX19Crypto : IMethodHook
{
    // 重构 X19 相关算法
    public const string ClassName = "WPFLauncher.Util.ss";

    [HookMethod(ClassName, "b")]
    public static byte[] HttpEncrypt(string path, string body, out string key)
    {
        var result = X19Crypt.HttpEncrypt(Encoding.UTF8.GetBytes(body));
        var x19Key = X19Crypt.PickKey(result[result.Length - 1]);
        key = Encoding.UTF8.GetString(x19Key);
        if (WpfConfig.IsDebug)
            WpfConfig.DefaultLogger.Info(
                $"HttpEncrypt: Path: {path} Body: {body}, EncryptBytesCount: {result.Length}, EncryptKey: {key}");
        return result;
    }

    [HookMethod(ClassName, "c")]
    public static string HttpDecrypt(byte[] encryptBody, out string key)
    {
        var result = X19Crypt.DecryptX19Body(encryptBody);
        var x19Key = X19Crypt.PickKey(encryptBody[encryptBody.Length - 1]);
        key = Encoding.UTF8.GetString(x19Key);
        if (WpfConfig.IsDebug)
            WpfConfig.DefaultLogger.Info(
                $"HttpDecrypt: EncryptBodyCount: {encryptBody.Length}, DecryptBody: {result}, EncryptKey: {key}");
        return result;
    }

    [OriginalMethod]
    public static string ParseResponseLogin(byte[] encryptBody, out string key)
    {
        key = "";
        return "";
    }


    [HookMethod(ClassName, "d")]
    public static string ParseResponseLoginHook(byte[] encryptBody, out string key)
    {
        ParseResponseLogin(encryptBody, out key);
        var result = X19Crypt.DecryptX19Body(encryptBody);
        var x19Key = X19Crypt.PickKey(encryptBody[encryptBody.Length - 1]);
        key = Encoding.UTF8.GetString(x19Key);
        if (WpfConfig.IsDebug)
            WpfConfig.DefaultLogger.Info(
                $"ParseResponseLogin: EncryptBodyCount: {encryptBody.Length}, DecryptBody: {result}, EncryptKey: {key}");
        return result;
    }

    [HookMethod(ClassName, "e")]
    public static string ComputeDynamicToken(string path, string body)
    {
        var result = X19Crypt.ComputeDynamicToken(path, body);
        if (WpfConfig.IsDebug)
            WpfConfig.DefaultLogger.Info(
                $"DynamicToken: Body: {body}, Path: {path}, UserToken: {result}, UserId: {X19Crypt.UserId}");

        return result;
    }

    [HookMethod(ClassName, "f")]
    public static string GetH5Token()
    {
        var result = X19Crypt.GetH5Token();
        if (WpfConfig.IsDebug) WpfConfig.DefaultLogger.Info($"GetH5Token: Token: {X19Crypt.Token}, H5Token: {result}");

        return result;
    }
}