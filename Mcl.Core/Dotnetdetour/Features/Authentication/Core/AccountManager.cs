using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.Features.Authentication.Core; // 对应上文重构的命名空间

public static class AccountManager
{
    private const string AccountsFileName = "accounts.json";
    private static readonly string AccountsFilePath = Path.Combine(Environment.CurrentDirectory, AccountsFileName);
    
    private static List<AccountInfo> _accounts;
    private static readonly object _lock = new();
    private static bool _migrated;

    public static List<AccountInfo> Accounts
    {
        get
        {
            if (_accounts == null) Load();
            return _accounts;
        }
    }

    public static void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(AccountsFilePath))
                {
                    var json = File.ReadAllText(AccountsFilePath);
                    _accounts = JsonConvert.DeserializeObject<List<AccountInfo>>(json) ?? new List<AccountInfo>();
                }
                else
                {
                    _accounts = new List<AccountInfo>();
                }
            }
            catch
            {
                _accounts = new List<AccountInfo>();
            }

            if (!_migrated)
            {
                _migrated = true;
                MigrateOldFiles();
            }
        }
    }

    public static void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonConvert.SerializeObject(_accounts ?? new List<AccountInfo>(), Formatting.Indented);
                File.WriteAllText(AccountsFilePath, json);
            }
            catch (Exception ex)
            {
                // WpfConfig.DefaultLogger.Error($"[AccountManager] 保存账号失败: {ex.Message}");
                Console.WriteLine($"[AccountManager] 保存账号失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 自动将旧的 cookies.txt 和 4399.txt 迁移到新的账号系统中
    /// </summary>
    private static void MigrateOldFiles()
    {
        bool needsSave = false;

        try
        {
            // 迁移 Cookie
            var cookiePath = Path.Combine(Environment.CurrentDirectory, "cookies.txt");
            if (File.Exists(cookiePath))
            {
                var cookieData = File.ReadAllText(cookiePath).Trim();
                if (!string.IsNullOrEmpty(cookieData) && cookieData != "off" && !cookieData.StartsWith("UserName----"))
                {
                    if (!_accounts.Any(a => a.Type == AccountType.Cookie && a.CookieData == cookieData))
                    {
                        _accounts.Add(new AccountInfo
                        {
                            Name = GetAvailableName("旧Cookie账号"),
                            Type = AccountType.Cookie,
                            CookieData = cookieData,
                            Notes = "从 cookies.txt 自动导入"
                        });
                        needsSave = true;
                    }
                }
            }

            // 迁移 4399
            var legacy4399Path = Path.Combine(Environment.CurrentDirectory, "4399.txt");
            if (File.Exists(legacy4399Path))
            {
                var legacy4399Data = File.ReadAllText(legacy4399Path).Trim();
                if (!string.IsNullOrEmpty(legacy4399Data) && legacy4399Data != "off")
                {
                    var parts = legacy4399Data.Split(new[] { "----" }, StringSplitOptions.None);
                    if (parts.Length == 2 && !_accounts.Any(a => a.Type == AccountType._4399 && a.Username == parts[0]))
                    {
                        _accounts.Add(new AccountInfo
                        {
                            Name = GetAvailableName("旧4399账号"),
                            Type = AccountType._4399,
                            Username = parts[0],
                            Password = parts[1],
                            Notes = "从 4399.txt 自动导入"
                        });
                        needsSave = true;
                    }
                }
            }

            if (needsSave)
            {
                Save();
                // WpfConfig.DefaultLogger.Info("[AccountManager] 已成功导入旧版账号文件");
            }
        }
        catch (Exception ex)
        {
            // WpfConfig.DefaultLogger.Error($"[AccountManager] 迁移旧账号失败: {ex.Message}");
        }
    }

    public static void Add(AccountInfo account)
    {
        lock (_lock)
        {
            account.CreatedAt = DateTime.Now;
            account.LastUsed = DateTime.Now;
            Accounts.Add(account);
            Save();
        }
    }

    public static void Update(string originalName, AccountInfo updatedAccount)
    {
        lock (_lock)
        {
            var existing = Accounts.FirstOrDefault(a => a.Name == originalName);
            if (existing != null)
            {
                existing.Name = updatedAccount.Name;
                existing.Type = updatedAccount.Type;
                existing.CookieData = updatedAccount.CookieData;
                existing.Username = updatedAccount.Username;
                existing.Password = updatedAccount.Password;
                existing.PhoneNumber = updatedAccount.PhoneNumber;
                existing.DeviceId = updatedAccount.DeviceId;
                existing.Notes = updatedAccount.Notes;
                Save();
            }
        }
    }

    public static void Delete(string name)
    {
        lock (_lock)
        {
            if (Accounts.RemoveAll(a => a.Name == name) > 0)
            {
                Save();
            }
        }
    }

    public static void MarkUsed(AccountInfo account)
    {
        if (account == null) return;
        
        lock (_lock)
        {
            var existing = Accounts.FirstOrDefault(a => a.Name == account.Name);
            if (existing != null)
            {
                existing.LastUsed = DateTime.Now;
                Save();
            }
        }
    }

    public static AccountInfo FindByName(string name)
    {
        lock (_lock)
        {
            return Accounts.FirstOrDefault(a => a.Name == name);
        }
    }

    public static List<AccountInfo> GetByType(AccountType type)
    {
        lock (_lock)
        {
            return Accounts.Where(a => a.Type == type)
                           .OrderByDescending(a => a.LastUsed)
                           .ToList();
        }
    }

    public static List<AccountInfo> GetAllSorted()
    {
        lock (_lock)
        {
            return Accounts.OrderByDescending(a => a.LastUsed).ToList();
        }
    }

    public static string GetAvailableName(string baseName, string ignoredOriginalName = null)
    {
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "账号";
        string candidate = baseName.Trim();
        
        lock (_lock)
        {
            int index = 2;
            while (Accounts.Any(a => a.Name == candidate && a.Name != ignoredOriginalName))
            {
                candidate = $"{baseName.Trim()} ({index})";
                index++;
            }
        }

        return candidate;
    }
}