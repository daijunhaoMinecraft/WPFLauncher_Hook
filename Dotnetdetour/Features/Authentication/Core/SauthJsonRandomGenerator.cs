using System;
using System.Text;

namespace Mcl.Core.Dotnetdetour.Tools;

public static class SauthJsonRandomGenerator
{
    private static readonly Random Rand = new Random();
    private const string AlphaNum = "abcdefghijklmnopqrstuvwxyz0123456789";
    private const string Hex = "0123456789ABCDEF";

    /// <summary>
    /// 生成一个与示例结构相同但完全随机的 sauthJson 字符串
    /// </summary>
    public static string Generate()
    {
        string gameid = "19";
        string loginChannel = "netease";
        string appChannel = "netease";
        string platform = "pc";
        string sdkuid = RandomString(16, AlphaNum);           // 16位小写字母数字
        string sessionid = "";                                // 原字段为空，保持空
        string sdkVersion = $"4.10.0";
        string udid = RandomHexString(32);                    // 32位大写十六进制
        string deviceid = RandomString(13, AlphaNum) + "-d";  // 类似 amawr5aaaw4sv-d
        string aimInfo = "";
        string clientLoginSn = RandomHexString(32);
        string gasToken = "";
        string sourcePlatform = platform;                     // 与 platform 保持一致
        string ip = GenerateRandomIp();
        string getAccessToken = "1";                          // 固定值，按需修改
        string accessToken = "";

        // ---- 手动拼接 JSON（避免引入第三方库） ----
        string json = 
            "{" +
            $"\"gameid\":\"{gameid}\"," +
            $"\"login_channel\":\"{loginChannel}\"," +
            $"\"app_channel\":\"{appChannel}\"," +
            $"\"platform\":\"{platform}\"," +
            $"\"sdkuid\":\"{sdkuid}\"," +
            $"\"sessionid\":\"{sessionid}\"," +
            $"\"sdk_version\":\"{sdkVersion}\"," +
            $"\"udid\":\"{udid}\"," +
            $"\"deviceid\":\"{deviceid}\"," +
            $"\"aim_info\":\"{aimInfo}\"," +
            $"\"client_login_sn\":\"{clientLoginSn}\"," +
            $"\"gas_token\":\"{gasToken}\"," +
            $"\"source_platform\":\"{sourcePlatform}\"," +
            $"\"ip\":\"{ip}\"," +
            $"\"get_access_token\":\"{getAccessToken}\"," +
            $"\"access_token\":\"{accessToken}\"" +
            "}";

        return json;
    }

    // 辅助：随机字符串（小写字母+数字）
    private static string RandomString(int length, string chars)
    {
        if (length <= 0) return "";
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(chars[Rand.Next(chars.Length)]);
        return sb.ToString();
    }

    // 辅助：随机大写十六进制字符串
    private static string RandomHexString(int length)
    {
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(Hex[Rand.Next(Hex.Length)]);
        return sb.ToString();
    }

    // 辅助：从多个选项中随机选取一个
    private static string PickRandom(params string[] options) =>
        options[Rand.Next(options.Length)];

    // 辅助：生成随机内网/公网IP（避免生成 0.0.0.0）
    private static string GenerateRandomIp()
    {
        return $"{Rand.Next(1, 255)}.{Rand.Next(0, 256)}.{Rand.Next(0, 256)}.{Rand.Next(1, 255)}";
    }
}