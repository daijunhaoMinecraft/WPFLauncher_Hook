using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Core;

public enum AccountType
{
    Cookie,
    _4399,
    Phone,
    Email
}

public class AccountInfo
{
    public AccountInfo()
    {
        CreatedAt = DateTime.Now;
        LastUsed = DateTime.Now;
    }

    public string Name { get; set; }
    public AccountType Type { get; set; }
    public string CookieData { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string PhoneNumber { get; set; }
    public string DeviceId { get; set; }
    public DateTime LastUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Notes { get; set; }

    [JsonIgnore]
    public string TypeDisplay => Type switch
    {
        AccountType.Cookie => "Cookie",
        AccountType._4399 => "4399",
        AccountType.Phone => "手机号",
        AccountType.Email => "网易邮箱",
        _ => "未知"
    };

    [JsonIgnore]
    public string Summary
    {
        get
        {
            return Type switch
            {
                AccountType.Cookie => string.IsNullOrEmpty(CookieData) ? "(空)" :
                    (CookieData.Length > 60 ? CookieData.Substring(0, 60) + "..." : CookieData),
                AccountType.Phone => string.IsNullOrEmpty(PhoneNumber) ? "(空)" : PhoneNumber,
                AccountType.Email => string.IsNullOrEmpty(Username) ? "(空)" : Username,
                _ => string.IsNullOrEmpty(Username) ? "(空)" : Username
            };
        }
    }

    [JsonIgnore] 
    public string LastUsedDisplay => LastUsed.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 提供深度克隆，用于在 UI 编辑时避免直接修改原始实例
    /// </summary>
    public AccountInfo Clone()
    {
        return (AccountInfo)this.MemberwiseClone();
    }

    public override string ToString() => $"{Name} [{TypeDisplay}]";
    
    public static class CookieValidator
    {
        public static bool ValidateSauth(string cookieData, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(cookieData))
            {
                error = "Cookie 不能为空";
                return false;
            }

            try
            {
                // 1. 检查外层是否是合法 JSON
                var root = JObject.Parse(cookieData);
            
                // 2. 检查是否包含 sauth_json 字段
                var sauthToken = root["sauth_json"];
                if (sauthToken == null)
                {
                    error = "JSON 中未找到 'sauth_json' 字段。";
                    return false;
                }

                // 3. 检查 sauth_json 的值是否为字符串
                if (sauthToken.Type != JTokenType.String)
                {
                    error = "'sauth_json' 的值必须是字符串类型。";
                    return false;
                }

                // 4. 检查该字符串内部是否是合法的 JSON
                string innerJson = sauthToken.ToString();
                JObject.Parse(innerJson); 

                return true;
            }
            catch (JsonReaderException)
            {
                error = "格式错误：提供的不是有效的 JSON 字符串。";
                return false;
            }
            catch (Exception ex)
            {
                error = $"校验异常: {ex.Message}";
                return false;
            }
        }
    }
}