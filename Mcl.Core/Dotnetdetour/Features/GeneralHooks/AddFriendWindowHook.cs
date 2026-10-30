using System;
using System.Windows;
using System.Windows.Input;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.UI.Windows;
using Mcl.Core.Dotnetdetour.Utilities.Diagnostics;
using WPFLauncher.View;
using WPFLauncher.View.Chat.ChatPanelUC;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

internal sealed class AddFriendWindowHook : IMethodHook
{
    [OriginalMethod]
    private static void ChatAddFriendOriginal(ChatMainWindow instance, object sender, RoutedEventArgs args)
    {
    }

    [HookMethod("WPFLauncher.View.ChatMainWindow", "i", "ChatAddFriendOriginal")]
    private static void ChatAddFriendHook(ChatMainWindow instance, object sender, RoutedEventArgs args)
    {
        ShowReplacement(instance);
    }

    [OriginalMethod]
    private static void FriendListAddFriendOriginal(FriendListUC instance, object sender,
        MouseButtonEventArgs args)
    {
    }

    [HookMethod("WPFLauncher.View.Chat.ChatPanelUC.FriendListUC", "p",
        "FriendListAddFriendOriginal")]
    private static void FriendListAddFriendHook(FriendListUC instance, object sender,
        MouseButtonEventArgs args)
    {
        if (args == null || args.LeftButton != MouseButtonState.Pressed) return;
        ShowReplacement(instance?.View);
    }

    private static void ShowReplacement(Window owner)
    {
        try
        {
            AddFriendWindow.ShowFor(owner);
        }
        catch (Exception exception)
        {
            PluginLog.Error("Chat", exception, "Showing the replacement add-friend window failed.");
        }
    }
}
