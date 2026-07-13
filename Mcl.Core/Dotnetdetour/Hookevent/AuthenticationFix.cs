using System;
using Mcl.Core.Dotnetdetour.Tools;
using Mcl.Core.Network.Interface;
using Newtonsoft.Json.Linq;
using WPFLauncher.Code;
using WPFLauncher.Network.Launcher;

namespace Mcl.Core.Dotnetdetour.Hookevent;

// 修复问题: 部分4399小号会出现无法购买物品情况

public class AuthenticationFix : IMethodHook
{
    [OriginalMethod]
    public static INetResponse<EntityDetailResponse<AuthenticationEntity>> Authentication(string sauthJson, string version, string aid, string otpToken, string otpPwd)
    {
        return null;
    }
    
    [HookMethod("WPFLauncher.Network.Launcher.acp", "b", "Authentication")]
    public static INetResponse<EntityDetailResponse<AuthenticationEntity>> AuthenticationHook(string sauthJson, string version, string aid, string otpToken, string otpPwd)
    {
        sauthJson = SauthJsonRandomGenerator.Generate();
        Console.WriteLine($"sauth_json随机化(authentication-otp): {sauthJson}");
        INetResponse<EntityDetailResponse<AuthenticationEntity>> response =
            Authentication(sauthJson, version, aid, otpToken, otpPwd);
        
        if (response.Data.code == 0)   // 假设 0 代表登录成功（可按实际接口修改）
        {
            Logger.Success($"登录成功, 返回代码: {response.Data.code}, UserId: {response.Data.entity.entity_id}, UserToken: {response.Data.entity.token}");
            // 登录成功后的逻辑（根据你的需要填写，这里留空）
        }
        else if (response.Data.code == 29)   // 账号被封禁
        {
            JObject detailsObj = JObject.Parse(response.Data.details.ToString());
            Logger.Error($"因 {detailsObj["ban_msg"]} 您的账号被禁止登录游戏至 {X19Http.unix_timestamp_to(detailsObj["ban_to_ts"].ToObject<long>())}，{response.Data.message}!");
        }
        return response;
    }
}