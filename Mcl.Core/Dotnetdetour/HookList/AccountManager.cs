using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.HookList
{
    public static class AccountManager
    {
        private const string ACCOUNTS_FILE = "accounts.json";
        private static List<AccountInfo> _accounts;
        private static readonly object _lock = new object();
        private static bool _migrated = false;

        public static List<AccountInfo> Accounts
        {
            get
            {
                if (_accounts == null)
                {
                    Load();
                }
                return _accounts;
            }
        }

        public static void Load()
        {
            lock (_lock)
            {
                string filePath = Path.Combine(Environment.CurrentDirectory, ACCOUNTS_FILE);
                if (File.Exists(filePath))
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        _accounts = JsonConvert.DeserializeObject<List<AccountInfo>>(json) ?? new List<AccountInfo>();
                    }
                    catch
                    {
                        _accounts = new List<AccountInfo>();
                    }
                }
                else
                {
                    _accounts = new List<AccountInfo>();
                }

                if (!_migrated)
                {
                    MigrateOldFiles();
                    _migrated = true;
                }
            }
        }

        public static void Save()
        {
            lock (_lock)
            {
                string filePath = Path.Combine(Environment.CurrentDirectory, ACCOUNTS_FILE);
                try
                {
                    string json = JsonConvert.SerializeObject(_accounts ?? new List<AccountInfo>(), Formatting.Indented);
                    File.WriteAllText(filePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AccountManager] 保存账号失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Auto-migrate old cookies.txt and 4399.txt into the new account system
        /// </summary>
        private static void MigrateOldFiles()
        {
            try
            {
                string cookiePath = Path.Combine(Environment.CurrentDirectory, "cookies.txt");
                if (File.Exists(cookiePath))
                {
                    string cookieData = File.ReadAllText(cookiePath).Trim();
                    if (!string.IsNullOrEmpty(cookieData) && cookieData != "off" && !cookieData.StartsWith("UserName----"))
                    {
                        bool exists = _accounts.Any(a => a.Type == AccountType.Cookie && a.CookieData == cookieData);
                        if (!exists)
                        {
                            _accounts.Add(new AccountInfo
                            {
                                Name = "旧Cookie账号(自动导入)",
                                Type = AccountType.Cookie,
                                CookieData = cookieData,
                                Notes = "从 cookies.txt 自动导入"
                            });
                            Console.WriteLine("[AccountManager] 已从 cookies.txt 导入旧Cookie账号");
                        }
                    }
                }

                string _4399Path = Path.Combine(Environment.CurrentDirectory, "4399.txt");
                if (File.Exists(_4399Path))
                {
                    string _4399Data = File.ReadAllText(_4399Path).Trim();
                    if (!string.IsNullOrEmpty(_4399Data) && _4399Data != "off")
                    {
                        string[] parts = _4399Data.Split(new string[] { "----" }, StringSplitOptions.None);
                        if (parts.Length == 2)
                        {
                            bool exists = _accounts.Any(a => a.Type == AccountType._4399 && a.Username == parts[0]);
                            if (!exists)
                            {
                                _accounts.Add(new AccountInfo
                                {
                                    Name = "旧4399账号(自动导入)",
                                    Type = AccountType._4399,
                                    Username = parts[0],
                                    Password = parts[1],
                                    Notes = "从 4399.txt 自动导入"
                                });
                                Console.WriteLine("[AccountManager] 已从 4399.txt 导入旧4399账号");
                            }
                        }
                    }
                }

                if (_accounts.Any(a => a.Notes != null && a.Notes.Contains("自动导入")))
                {
                    Save();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AccountManager] 迁移旧账号失败: {ex.Message}");
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

        public static void Update(string originalName, AccountInfo account)
        {
            lock (_lock)
            {
                var existing = Accounts.FirstOrDefault(a => a.Name == originalName);
                if (existing != null)
                {
                    existing.Name = account.Name;
                    existing.Type = account.Type;
                    existing.CookieData = account.CookieData;
                    existing.Username = account.Username;
                    existing.Password = account.Password;
                    existing.PhoneNumber = account.PhoneNumber;
                    existing.DeviceId = account.DeviceId;
                    existing.Notes = account.Notes;
                    Save();
                }
            }
        }

        public static void Delete(string name)
        {
            lock (_lock)
            {
                Accounts.RemoveAll(a => a.Name == name);
                Save();
            }
        }

        public static void MarkUsed(AccountInfo account)
        {
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
            return Accounts.FirstOrDefault(a => a.Name == name);
        }

        public static List<AccountInfo> GetByType(AccountType type)
        {
            return Accounts.Where(a => a.Type == type).OrderByDescending(a => a.LastUsed).ToList();
        }

        public static List<AccountInfo> GetAllSorted()
        {
            return Accounts.OrderByDescending(a => a.LastUsed).ToList();
        }

        public static string GetAvailableName(string baseName, string ignoredOriginalName = null)
        {
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "账号";
            }

            string baseNameTrimmed = baseName.Trim();
            string candidate = baseNameTrimmed;
            int index = 2;
            while (Accounts.Any(a => a.Name == candidate && a.Name != ignoredOriginalName))
            {
                candidate = $"{baseNameTrimmed} ({index})";
                index++;
            }
            return candidate;
        }
    }
}
