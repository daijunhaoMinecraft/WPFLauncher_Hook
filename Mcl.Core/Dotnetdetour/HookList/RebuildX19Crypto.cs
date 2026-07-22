using System.Text;
using Mcl.Core.Tools;

namespace Mcl.Core.Dotnetdetour.HookList;

public class RebuildX19Crypto : IMethodHook
{
    // 重构 X19 相关算法
    public const string ClassName = "WPFLauncher.Util.ss";

    [HookMethod(ClassName, "b")]
    public unsafe static byte[] HttpEncrypt(string path, string body, out string key)
    {
        byte[] result = X19Crypt.HttpEncrypt(Encoding.UTF8.GetBytes(body));
        byte[] x19Key = X19Crypt.PickKey(result[result.Length - 1]);
        key = Encoding.UTF8.GetString(x19Key);
        if (WpfConfig.IsDebug)
        {
            WpfConfig.DefaultLogger.Info($"HttpEncrypt: Path: {path} Body: {body}, EncryptBytesCount: {result.Length}, EncryptKey: {key}");
        }
        return result;
    }

    [HookMethod(ClassName, "c")]
    public static string HttpDecrypt(byte[] encryptBody, out string key)
    {
        string result = X19Crypt.DecryptX19Body(encryptBody);
        byte[] x19Key = X19Crypt.PickKey(encryptBody[encryptBody.Length - 1]);
        key = Encoding.UTF8.GetString(x19Key);
        if (WpfConfig.IsDebug)
        {
            WpfConfig.DefaultLogger.Info(
                $"HttpDecrypt: EncryptBodyCount: {encryptBody.Length}, DecryptBody: {result}, EncryptKey: {key}");
        }
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
        string result = X19Crypt.DecryptX19Body(encryptBody);
        byte[] x19Key = X19Crypt.PickKey(encryptBody[encryptBody.Length - 1]);
        key = Encoding.UTF8.GetString(x19Key);
        if (WpfConfig.IsDebug)
        {
            WpfConfig.DefaultLogger.Info($"ParseResponseLogin: EncryptBodyCount: {encryptBody.Length}, DecryptBody: {result}, EncryptKey: {key}");
        }
        return result;
    }

    [HookMethod(ClassName, "e")]
    public static string ComputeDynamicToken(string path, string body)
    {
        string result = X19Crypt.ComputeDynamicToken(path, body);
        if (WpfConfig.IsDebug)
        {
            WpfConfig.DefaultLogger.Info(
                $"DynamicToken: Body: {body}, Path: {path}, UserToken: {result}, UserId: {X19Crypt.UserId}");
        }

        return result;
    }

    [HookMethod(ClassName, "f")]
    public static string GetH5Token()
    {
        string result = X19Crypt.GetH5Token();
        if (WpfConfig.IsDebug)
        {
            WpfConfig.DefaultLogger.Info($"GetH5Token: Token: {X19Crypt.Token}, H5Token: {result}");
        }

        return result;
    }
}