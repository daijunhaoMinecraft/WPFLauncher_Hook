using System;
using Mcl.Core.NeteaseProtocol;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Mcl.Core.Dotnetdetour.HookList;

public class OptizimeRandomNickName : IMethodHook
{
    private static List<string> _cachedNicknames = null;
    private static int _currentIndex = 0;
    private static readonly object _lock = new object();

    [OriginalMethod]
    public static string GetRandomNickName(string lastRandomName = null)
    {
        return "";
    }
    
    
    // 优化随机名称
    [HookMethod("WPFLauncher.Util.to", "d", "GetRandomNickName")]
    public static string GetRandomNickNameHook(string lastRandomName = null)
    {
        lock (_lock)
        {
            // 若缓存为空或索引超出列表范围，重新请求 API 刷新昵称池
            if (_cachedNicknames == null || _currentIndex >= _cachedNicknames.Count)
            {
                try
                {
                    WpfConfig.DefaultLogger?.Info("开始请求随机昵称 API ...");
                    string response = X19Http.Post(
                        "/nickname-init/get-random-name",
                        "{}",
                        "https://g79apigatewayobt.minecraft.cn");

                    if (!string.IsNullOrEmpty(response))
                    {
                        JObject json = JObject.Parse(response);
                        var names = json["entity"]?["names"];
                        if (names != null)
                        {
                            _cachedNicknames = new List<string>();
                            foreach (var item in names)
                            {
                                string name = item["name"]?.ToString();
                                if (!string.IsNullOrEmpty(name))
                                    _cachedNicknames.Add(name);
                            }
                            _currentIndex = 0;
                            WpfConfig.DefaultLogger?.Info($"成功获取 {_cachedNicknames.Count} 个随机昵称并缓存");
                        }
                        else
                        {
                            WpfConfig.DefaultLogger?.Warn("API 返回的昵称列表为空");
                        }
                    }
                    else
                    {
                        WpfConfig.DefaultLogger?.Warn("随机昵称 API 返回空响应");
                    }
                }
                catch (Exception ex)
                {
                    WpfConfig.DefaultLogger?.Error($"请求随机昵称失败: {ex.Message}");
                    // 解析或请求异常时，若已有旧缓存则不清空，返回一个默认值保证不崩溃
                    if (_cachedNicknames == null || _cachedNicknames.Count == 0)
                    {
                        WpfConfig.DefaultLogger?.Info("无可用缓存，调用原始随机昵称方法");
                        return GetRandomNickName(lastRandomName);
                    }
                    else
                    {
                        WpfConfig.DefaultLogger?.Warn("请求失败但存在旧缓存，继续使用缓存");
                    }
                }
            }

            // 如果缓存仍然无数据，返回默认昵称
            if (_cachedNicknames == null || _cachedNicknames.Count == 0)
            {
                WpfConfig.DefaultLogger?.Info("昵称缓存为空，调用原始随机昵称方法");
                return GetRandomNickName(lastRandomName);
            }

            // 按顺序取出一个昵称并前移索引
            string result = _cachedNicknames[_currentIndex];
            _currentIndex++;
            WpfConfig.DefaultLogger?.Info($"返回顺序昵称[{_currentIndex - 1}/{_cachedNicknames.Count}]: {result}");
            return result;
        }
    }
}