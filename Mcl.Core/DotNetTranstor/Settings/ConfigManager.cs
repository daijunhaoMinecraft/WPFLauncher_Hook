using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace DotNetTranstor.Hookevent
{
    public class ConfigEntry
    {
        public string Key { get; set; }        // 必须对应 Path_Bool 里的静态字段名
        public string Description { get; set; } // UI 上显示的中文名
        public Type FieldType { get; set; }    // 变量类型
        public string Category { get; set; } // 新增：分类 (如 "基础", "联机", "高级")
    }

    public static class ConfigManager
    {
        private const string CONFIG_FILE = "config.json";

        public static readonly List<ConfigEntry> Registry = new List<ConfigEntry>
        {
            // 基础设置
            new ConfigEntry { Key = "IsBypassGameUpdate_Bedrock", Description = "绕过基岩版更新", FieldType = typeof(bool), Category = "基础设置" },
            new ConfigEntry { Key = "IsEnableX64mc", Description = "使用X64版本(基岩)", FieldType = typeof(bool), Category = "基础设置" },
            new ConfigEntry { Key = "IsDebug", Description = "详细日志模式", FieldType = typeof(bool), Category = "基础设置" },
            new ConfigEntry { Key = "IsStartWebSocket", Description = "启用Web服务器", FieldType = typeof(bool), Category = "基础设置" },
            new ConfigEntry { Key = "HttpPort", Description = "Web服务器端口", FieldType = typeof(int), Category = "基础设置" },
            
            // 联机大厅设置
            new ConfigEntry { Key = "MaxRoomCount", Description = "最大房间数量", FieldType = typeof(int), Category = "联机大厅设置" },
            new ConfigEntry { Key = "IsCustomIP", Description = "自定义IP进入服务器", FieldType = typeof(bool), Category = "联机大厅设置" },
            new ConfigEntry { Key = "NoTwoExitMessage", Description = "禁用退出二次确认", FieldType = typeof(bool), Category = "联机大厅设置" },
            new ConfigEntry { Key = "EnableRoomBlacklist", Description = "启用房间黑名单", FieldType = typeof(bool), Category = "联机大厅设置" },
            new ConfigEntry { Key = "AlwaysSaveWorld", Description = "保存房间提醒", FieldType = typeof(bool), Category = "联机大厅设置" },
            
            new ConfigEntry { Key = "UseNetworkMode", Description = "使用组网模式", FieldType = typeof(bool), Category = "本地联机" },

            // 模组与高级
            // new ConfigEntry { Key = "EnableModsInject", Description = "启用模组注入", FieldType = typeof(bool), Category = "高级功能" },
            new ConfigEntry { Key = "NeteaseUpdateDomainhttp", Description = "网易更新域名", FieldType = typeof(string), Category = "高级功能" },
            
            // Experiment
            new ConfigEntry { Key = "IsDownloadMultiConfig", Description = "启用多线程下载", FieldType = typeof(bool), Category = "实验功能" },
            new ConfigEntry { Key = "MaxThread", Description = "下载多线程数", FieldType = typeof(int), Category = "实验功能" },
            new ConfigEntry { Key = "LimitDownload", Description = "大小限制(小于此大小即为小文件, 只使用单线程下载, 单位MB)", FieldType = typeof(int), Category = "实验功能" },
        };

        // 自动保存 Path_Bool 里的值到 JSON
        public static void Save()
        {
            var data = new Dictionary<string, object>();
            foreach (var item in Registry)
            {
                var field = typeof(Path_Bool).GetField(item.Key, BindingFlags.Public | BindingFlags.Static);
                if (field != null) data[item.Key] = field.GetValue(null);
            }
            File.WriteAllText(CONFIG_FILE, JsonConvert.SerializeObject(data, Formatting.Indented));
            Console.WriteLine("[Config] 配置已保存到 JSON");
        }

        // 自动从 JSON 加载值到 Path_Bool
        public static void Load()
        {
            if (!File.Exists(CONFIG_FILE)) return;
            try
            {
                var json = File.ReadAllText(CONFIG_FILE);
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                foreach (var item in Registry)
                {
                    if (data.TryGetValue(item.Key, out object val))
                    {
                        var field = typeof(Path_Bool).GetField(item.Key, BindingFlags.Public | BindingFlags.Static);
                        if (field != null)
                        {
                            // 自动处理 Json 反序列化时的类型转换
                            object convertedVal = Convert.ChangeType(val, item.FieldType);
                            field.SetValue(null, convertedVal);
                        }
                    }
                }
                Console.WriteLine("[Config] 配置加载成功");
            }
            catch (Exception ex) { Console.WriteLine("[Config] 加载失败: " + ex.Message); }
        }

        // 自动为 Path_Bool 的变量生成 UI 控件
        public static void BuildUI(StackPanel container, Dictionary<string, FrameworkElement> controlMap)
        {
            foreach (var item in Registry)
            {
                var field = typeof(Path_Bool).GetField(item.Key, BindingFlags.Public | BindingFlags.Static);
                var currentVal = field.GetValue(null);

                if (item.FieldType == typeof(bool))
                {
                    var cb = new CheckBox { Content = item.Description, IsChecked = (bool)currentVal, Margin = new Thickness(0, 5, 0, 5), FontSize = 14 };
                    container.Children.Add(cb);
                    controlMap[item.Key] = cb;
                }
                else
                {
                    var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
                    sp.Children.Add(new TextBlock { Text = item.Description + ": ", Width = 150, VerticalAlignment = VerticalAlignment.Center });
                    var tb = new TextBox { Text = currentVal?.ToString(), Width = 200 };
                    sp.Children.Add(tb);
                    container.Children.Add(sp);
                    controlMap[item.Key] = tb;
                }
            }
        }
        
        // 获取当前所有配置的值 (用于 /config/get)
        public static Dictionary<string, object> GetCurrentConfigValues()
        {
            var data = new Dictionary<string, object>();
            foreach (var item in Registry)
            {
                var field = typeof(Path_Bool).GetField(item.Key, BindingFlags.Public | BindingFlags.Static);
                if (field != null) data[item.Key] = field.GetValue(null);
            }
            return data;
        }

        // 获取设置的元数据 (用于 /config/settingslist)
        // 返回格式: { Key: { desc: "中文名", type: "Boolean/Int32/String" } }
        public static Dictionary<string, object> GetMetadata()
        {
            return Registry.ToDictionary(
                x => x.Key, 
                x => (object)new { desc = x.Description, type = x.FieldType.Name }
            );
        }

        // 从 JSON 字符串批量更新 Path_Bool (用于 Web 保存)
        public static void UpdateFromJson(string json)
        {
            var updates = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            foreach (var key in updates.Keys)
            {
                var field = typeof(Path_Bool).GetField(key, BindingFlags.Public | BindingFlags.Static);
                var registryItem = Registry.FirstOrDefault(x => x.Key == key);
                if (field != null && registryItem != null)
                {
                    field.SetValue(null, Convert.ChangeType(updates[key], registryItem.FieldType));
                }
            }
            Save(); // 保存到文件
        }
    }
}