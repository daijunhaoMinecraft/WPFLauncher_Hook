using System;
using System.Collections.Generic;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using Newtonsoft.Json;
using WPFLauncher.Common;
using WPFLauncher.Manager;
using WPFLauncher.Manager.Configuration;
using WPFLauncher.Model;
using WPFLauncher.Util;
using WPFLauncher.View.Chat;

public class ChatNotificationHook : IMethodHook
{
    // ==========================================
    // 1. 核心拦截入口
    // ==========================================
    [HookMethod("WPFLauncher.Network.Service.acq", "i", "I_Original")]
    private void MCLauncherNotifyHook(UserM targetUser, aiv msg, uint targetUserId, bool isNotifyAllowed)
    {
        if (WpfConfig.ShowWindowsNotify)
        {
            // 如果不需要通知（正在查看该聊天、或开启了免打扰），则直接返回
            if (!ShouldNotify(targetUser, isNotifyAllowed)) return;

            // 解析消息文本（处理纯文本和表情包）
            string messageText = ParseMessageContent(msg);

            // 调度到主线程显示系统气泡通知
            ShowBalloonNotification(targetUser.DisplayName, messageText, targetUserId);
        }
        else
        {
            MCLauncherNotify(targetUser, msg, targetUserId, isNotifyAllowed);
        }
    }

    // ==========================================
    // 2. 通知前置条件判断与状态还原
    // ==========================================
    private bool ShouldNotify(UserM targetUser, bool isNotifyAllowed)
    {
        // 检查用户是否当前正在看这个人的聊天窗口，如果是，则不通知
        if (azf<arb>.Instance.w(targetUser)) return false;

        // 还原原程序的内部状态管理（防止未读消息等逻辑被破坏）
        try
        {
            ChatTabView chatTabView = azf<arb>.Instance.b();
            chatTabView?.d();
            chatTabView?.a(targetUser);
        }
        catch (Exception ex)
        {
            Console.WriteLine("更新聊天状态失败: " + ex.Message);
        }

        // 检查系统设置中是否开启了消息通知
        return azf<axi>.Instance.User.MsgNotify && isNotifyAllowed;
    }

    // ==========================================
    // 3. 消息内容解析 (分离文本与表情包逻辑)
    // ==========================================
    private string ParseMessageContent(aiv msg)
    {
        ChatMsgType type = (ChatMsgType)msg.Type;

        if (type == ChatMsgType.Text)
        {
            return msg.PlainText;
        }

        if (type == ChatMsgType.Emote)
        {
            try
            {
                // 解析表情的 JSON 获取名称
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(msg.Msg);
                if (dict != null && dict.TryGetValue("name", out object nameObj))
                {
                    return $"[{nameObj}]";
                }
            }
            catch
            {
                // 解析失败时降级
            }
        }

        // 其他类型 (如图片、文件等) 使用默认的资源提示符
        return $"[{tp.b(type, "resource")}]";
    }

    // ==========================================
    // 4. 显示 Windows 托盘气泡通知
    // ==========================================
    private void ShowBalloonNotification(string senderName, string content, uint targetUserId)
    {
        if (System.Windows.Application.Current == null) return;

        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                var notifyIcon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = System.Drawing.SystemIcons.Information,
                    Visible = true
                };

                // 点击通知气泡，自动打开对应的聊天窗口
                notifyIcon.BalloonTipClicked += (s, e) =>
                {
                    azf<arb>.Instance.ChatMainWindow.a(targetUserId);
                };

                // 发送通知 (持续时间参数在 Win10/11 中已被系统接管，仅作占位)
                notifyIcon.ShowBalloonTip(3000, $"{senderName} 发来消息", content, System.Windows.Forms.ToolTipIcon.Info);

                // 延迟清理资源，防止托盘区出现“幽灵图标”
                System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ => DisposeNotifyIcon(notifyIcon));
            }
            catch (Exception ex)
            {
                Console.WriteLine("显示托盘气泡失败: " + ex.Message);
            }
        }));
    }

    // ==========================================
    // 5. 安全释放 NotifyIcon 资源
    // ==========================================
    private void DisposeNotifyIcon(System.Windows.Forms.NotifyIcon icon)
    {
        if (icon == null) return;
        try
        {
            icon.Visible = false;
            icon.Dispose();
        }
        catch { }
    }

    // ==========================================
    // 6. 屏蔽原方法
    // ==========================================
    [OriginalMethod]
    private void MCLauncherNotify(UserM targetUser, aiv msg, uint targetUserId, bool isNotifyAllowed)
    {
        // 故意留空，彻底屏蔽掉原启动器自带的 WPF 弹窗
    }
}