using System;
using Newtonsoft.Json;

namespace DotNetTranstor.Hookevent
{
    public enum AccountType
    {
        Cookie,
        _4399,
        Phone
    }

    public class AccountInfo
    {
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

        public AccountInfo()
        {
            CreatedAt = DateTime.Now;
            LastUsed = DateTime.Now;
        }

        [JsonIgnore]
        public string TypeDisplay
        {
            get
            {
                switch (Type)
                {
                    case AccountType.Cookie: return "Cookie";
                    case AccountType._4399: return "4399";
                    case AccountType.Phone: return "手机号";
                    default: return "未知";
                }
            }
        }

        [JsonIgnore]
        public string Summary
        {
            get
            {
                if (Type == AccountType.Cookie)
                {
                    if (string.IsNullOrEmpty(CookieData)) return "(空)";
                    return CookieData.Length > 60 ? CookieData.Substring(0, 60) + "..." : CookieData;
                }
                else if (Type == AccountType.Phone)
                {
                    return string.IsNullOrEmpty(PhoneNumber) ? "(空)" : PhoneNumber;
                }
                else
                {
                    return string.IsNullOrEmpty(Username) ? "(空)" : Username;
                }
            }
        }

        [JsonIgnore]
        public string LastUsedDisplay => LastUsed.ToString("yyyy-MM-dd HH:mm:ss");

        public override string ToString()
        {
            return $"{Name} [{TypeDisplay}]";
        }
    }
}
