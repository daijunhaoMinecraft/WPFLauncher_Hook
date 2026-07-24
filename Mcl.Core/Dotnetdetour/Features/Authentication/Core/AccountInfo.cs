using System;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Core;

public enum AccountType
{
    Cookie,
    _4399,
    Phone,
    Email   // 新增网易邮箱类型
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
    public string Summary => Type switch
    {
        AccountType.Cookie => string.IsNullOrEmpty(CookieData) ? "(空)" :
            (CookieData.Length > 60 ? CookieData.Substring(0, 60) + "..." : CookieData),
        AccountType.Phone => string.IsNullOrEmpty(PhoneNumber) ? "(空)" : PhoneNumber,
        AccountType.Email => string.IsNullOrEmpty(Username) ? "(空)" : Username,
        AccountType._4399 => string.IsNullOrEmpty(Username) ? "(空)" : Username,
        _ => "(空)"
    };

    [JsonIgnore] 
    public string LastUsedDisplay => LastUsed.ToString("yyyy-MM-dd HH:mm:ss");

    public AccountInfo Clone()
    {
        return (AccountInfo)this.MemberwiseClone();
    }

    public override string ToString() => $"{Name} [{TypeDisplay}]";
}