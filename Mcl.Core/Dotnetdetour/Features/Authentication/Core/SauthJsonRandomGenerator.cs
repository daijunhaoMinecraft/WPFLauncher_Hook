using System;
using System.Text;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Core;

public static class SauthJsonRandomGenerator
{
    private const string AlphaNum = "abcdefghijklmnopqrstuvwxyz0123456789";
    private const string Hex = "0123456789ABCDEF";
    private static readonly Random Rand = new();

    /// <summary>
    ///     生成一个与示例结构相同但完全随机的 sauthJson 字符串
    /// </summary>
    public static string Generate()
    {
        var gameid = "19";
        var loginChannel = "netease";
        var appChannel = "netease";
        var platform = "pc";
        var sdkuid = RandomString(16, AlphaNum); // 16位小写字母数字
        var sessionid = ""; // 原字段为空，保持空
        var sdkVersion = "4.10.0";
        var udid = RandomHexString(32); // 32位大写十六进制
        var deviceid = RandomString(13, AlphaNum) + "-d"; // 类似 amawr5aaaw4sv-d
        var aimInfo = "";
        var clientLoginSn = RandomHexString(32);
        var gasToken = "";
        var sourcePlatform = platform; // 与 platform 保持一致
        var ip = GenerateRandomIp();
        var getAccessToken = "1"; // 固定值，按需修改
        var accessToken = "";

        // ---- 手动拼接 JSON（避免引入第三方库） ----
        var json =
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
        for (var i = 0; i < length; i++)
            sb.Append(chars[Rand.Next(chars.Length)]);
        return sb.ToString();
    }

    // 辅助：随机大写十六进制字符串
    private static string RandomHexString(int length)
    {
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
            sb.Append(Hex[Rand.Next(Hex.Length)]);
        return sb.ToString();
    }

    // 辅助：从多个选项中随机选取一个
    private static string PickRandom(params string[] options)
    {
        return options[Rand.Next(options.Length)];
    }

    // 辅助：生成随机内网/公网IP（避免生成 0.0.0.0）
    private static string GenerateRandomIp()
    {
        return $"{Rand.Next(1, 255)}.{Rand.Next(0, 256)}.{Rand.Next(0, 256)}.{Rand.Next(1, 255)}";
    }
}