using System;
using System.IO;
using System.Runtime.CompilerServices;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Utilities.Network;
using Newtonsoft.Json;

namespace Mcl.Core.Dotnetdetour.Features.AntiCheatBypass;

//去除网易存档加密功能
internal class DisableCppUpdate : IMethodHook
{
    [OriginalMethod]
    public bool No_Update(bool skipValidation = true)
    {
        return false;
    }

    /// <summary>
    ///     检查本地是否存在有效的基岩版游戏文件
    /// </summary>
    private bool IsLocalBedrockGameExists()
    {
        try
        {
            if (Directory.Exists(WpfConfig.BedrockPath))
                foreach (var dir in Directory.GetDirectories(WpfConfig.BedrockPath))
                    if (File.Exists(Path.Combine(dir, "Minecraft.Windows.exe")))
                        return true;

            return false;
        }
        catch (Exception ex)
        {
            WpfConfig.DefaultLogger.Error($"检查本地游戏文件时发生异常: {ex.Message}");
            return false;
        }
    }

    [CompilerGenerated]
    [HookMethod("WPFLauncher.Model.Game.ale", "fn", "No_Update")]
    public bool CheckUpdate(bool skipValidation = true)
    {
        if (WpfConfig.IsStartWebSocket)
            WebSocketHelper.SendToClient(JsonConvert.SerializeObject(new
            {
                type = "IsBypassGameUpdate_Bedrock", BypassGameUpdate_Bedrock = WpfConfig.IsBypassGameUpdate_Bedrock,
                skipValidation
            }));

        if (WpfConfig.IsDebug)
            Console.WriteLine(
                $"[INFO_Bedrock]IsBypassGameUpdate_Bedrock:{WpfConfig.IsBypassGameUpdate_Bedrock},skipValidation:{skipValidation}");

        if (WpfConfig.IsBypassGameUpdate_Bedrock)
        {
            // --- 新增逻辑开始 ---
            if (!IsLocalBedrockGameExists())
            {
                WpfConfig.DefaultLogger.Warn("已启用跳过更新，但未在本地硬盘检测到有效的基岩版游戏文件 (Minecraft.Windows.exe)。");
                WpfConfig.DefaultLogger.Warn("将回退到原始更新检查逻辑。");

                // 返回原始方法的结果 (通常为 false，意味着需要更新或验证失败)
                return No_Update(skipValidation);
            }
            // --- 新增逻辑结束 ---

            // 如果检测通过，则正常绕过
            return true;
        }

        return No_Update(skipValidation);

        //  unreachable code removed
    }
}