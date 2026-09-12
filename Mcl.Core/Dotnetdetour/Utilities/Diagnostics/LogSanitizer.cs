using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Mcl.Core.Dotnetdetour.Utilities.Diagnostics;

/// <summary>Normalizes plugin diagnostics so console encoding and log injection cannot corrupt output.</summary>
public static class LogSanitizer
{
    private const string RedactedValue = "<redacted>";
    private static readonly Regex SensitiveAssignment = new Regex(
        @"(?ix)(?<key>password|passwd|cookie|sauth|token|authorization|secret|access_token|refresh_token)\s*[:=]\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^,;\s]+))",
        RegexOptions.Compiled);
    private static readonly Regex BearerToken = new Regex(@"(?i)\bBearer\s+[A-Za-z0-9._~+/=-]+", RegexOptions.Compiled);
    private static readonly Regex LegacyPrefix = new Regex(@"^\s*(?:\[[^\]]+\]\s*)+", RegexOptions.Compiled);
    private static readonly KeyValuePair<string, string>[] LegacyTranslations =
    {
        new KeyValuePair<string, string>("正在重新加入房间", "Rejoining room"),
        new KeyValuePair<string, string>("成功加入房间", "Room joined successfully"),
        new KeyValuePair<string, string>("创建房间黑名单文件夹", "Room blacklist directory created"),
        new KeyValuePair<string, string>("创建房间黑名单文件", "Room blacklist file created"),
        new KeyValuePair<string, string>("房间信息已更改", "Room information changed"),
        new KeyValuePair<string, string>("房间ID更改为", "Room ID changed to"),
        new KeyValuePair<string, string>("房间名称更改为", "Room name changed to"),
        new KeyValuePair<string, string>("玩家列表已发送给", "Player list sent to"),
        new KeyValuePair<string, string>("用户未配置 IP，启动中止", "User IP is not configured; startup aborted"),
        new KeyValuePair<string, string>("HTTP 服务器已启动", "HTTP server started"),
        new KeyValuePair<string, string>("HTTP 服务器已停止", "HTTP server stopped"),
        new KeyValuePair<string, string>("处理请求时发生错误", "Request handling failed"),
        new KeyValuePair<string, string>("保存配置失败", "Configuration save failed"),
        new KeyValuePair<string, string>("读取配置失败", "Configuration load failed"),
        new KeyValuePair<string, string>("无法读取房间列表", "Room list could not be loaded"),
        new KeyValuePair<string, string>("配置已保存", "Configuration saved"),
        new KeyValuePair<string, string>("配置加载成功", "Configuration loaded"),
        new KeyValuePair<string, string>("启动进程", "Starting process"),
        new KeyValuePair<string, string>("进程已退出", "Process exited"),
        new KeyValuePair<string, string>("强制结束进程失败", "Failed to terminate process"),
        new KeyValuePair<string, string>("自定义 JVM 参数", "Custom JVM arguments"),
        new KeyValuePair<string, string>("开始手机号登录", "Starting phone login"),
        new KeyValuePair<string, string>("使用缓存凭证登录", "Using cached credentials"),
        new KeyValuePair<string, string>("账号凭证提取失败", "Credential extraction failed"),
        new KeyValuePair<string, string>("Cookie注入失败", "Cookie injection failed"),
        new KeyValuePair<string, string>("登录异常", "Login exception"),
        new KeyValuePair<string, string>("登录成功", "Login succeeded"),
        new KeyValuePair<string, string>("更新检查失败", "Update check failed"),
        new KeyValuePair<string, string>("获取更新内容", "Loading update notes"),
        new KeyValuePair<string, string>("当前版本", "Current version"),
        new KeyValuePair<string, string>("最新版本", "Latest version"),
        new KeyValuePair<string, string>("网络请求失败", "Network request failed"),
        new KeyValuePair<string, string>("连接关闭", "Connection closed"),
        new KeyValuePair<string, string>("连接成功", "Connection succeeded"),
        new KeyValuePair<string, string>("发送数据", "Data sent"),
        new KeyValuePair<string, string>("收到消息", "Message received"),
        new KeyValuePair<string, string>("获取玩家列表", "Loading player list"),
        new KeyValuePair<string, string>("成功获取", "Loaded successfully"),
        new KeyValuePair<string, string>("无法找到", "Could not find"),
        new KeyValuePair<string, string>("发生异常", "Exception occurred"),
        new KeyValuePair<string, string>("操作失败", "Operation failed"),
        new KeyValuePair<string, string>("操作成功", "Operation succeeded")
    };

    public static string Format(string module, string message, int maxLength = 512, bool allowUnicode = false)
    {
        // var safeModule = SanitizeModule(module);
        var safeModule = module;
        // var safeMessage = Sanitize(allowUnicode ? message : TranslateLegacyText(message), maxLength, allowUnicode);
        // var safeMessage = allowUnicode ? message : TranslateLegacyText(message);
        var safeMessage = message;
        return $"[MCL][{safeModule}] {safeMessage}";
    }

    public static string Sanitize(string message, int maxLength = 512, bool allowUnicode = false)
    {
        if (string.IsNullOrEmpty(message)) return string.Empty;
        var redacted = RedactSensitiveValues(message);
        var builder = new StringBuilder(Math.Min(redacted.Length, maxLength));
        for (var index = 0; index < redacted.Length && builder.Length < maxLength; index++)
        {
            var character = redacted[index];
            switch (character)
            {
                case '\r': builder.Append(@"\r"); break;
                case '\n': builder.Append(@"\n"); break;
                case '\t': builder.Append(@"\t"); break;
                case '\\': builder.Append(@"\\"); break;
                default:
                    if (character >= 32 && character <= 126) builder.Append(character);
                    else if (allowUnicode) builder.Append(character);
                    else if (character <= 0xFFFF) builder.Append($@"\u{(int)character:x4}");
                    break;
            }
        }
        if (redacted.Length > 0 && builder.Length >= maxLength) builder.Append("...");
        return builder.ToString();
    }

    public static string SanitizeModule(string module)
    {
        if (string.IsNullOrWhiteSpace(module)) return "Core";
        var builder = new StringBuilder();
        foreach (var character in module.Trim())
            if ((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') ||
                (character >= '0' && character <= '9') || character == '_' || character == '-')
                builder.Append(character);
        return builder.Length == 0 ? "Core" : builder.ToString();
    }

    public static string StripLegacyPrefixes(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return string.Empty;
        // var stripped = LegacyPrefix.Replace(message, string.Empty);
        var stripped = message;
        return stripped.Replace("[INFO]", string.Empty)
            .Replace("[ERROR]", string.Empty)
            .Replace("[WARN]", string.Empty)
            .Replace("[Warning]", string.Empty)
            .Replace("[Success]", string.Empty)
            .Replace("[Exception]", string.Empty)
            .Trim();
    }

    private static string TranslateLegacyText(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;
        var translated = message;
        foreach (var pair in LegacyTranslations) translated = translated.Replace(pair.Key, pair.Value);
        return translated;
    }

    public static string CredentialSummary(string type, string value)
    {
        var safeType = Sanitize(type, 64);
        var raw = value ?? string.Empty;
        var digest = ComputeSha256(raw);
        var preview = raw.Length <= 4 ? string.Empty : Sanitize(raw.Substring(0, 2) + "..." + raw.Substring(raw.Length - 2), 32);
        return $"type={safeType} length={raw.Length} preview={preview} sha256={digest.Substring(0, 12)}...";
    }

    private static string RedactSensitiveValues(string message)
    {
        var redacted = SensitiveAssignment.Replace(message, match => match.Groups["key"].Value + "=" + RedactedValue);
        return BearerToken.Replace(redacted, "Bearer " + RedactedValue);
    }

    private static string ComputeSha256(string value)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var item in bytes) builder.Append(item.ToString("x2"));
            return builder.ToString();
        }
    }
}
