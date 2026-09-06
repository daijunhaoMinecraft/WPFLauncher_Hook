using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Mcl.Core.Dotnetdetour.Models.Entities;
using Mcl.Core.Dotnetdetour.Models.Entity;
using Net.Nekocurit.Cipher;

namespace Mcl.Core.Dotnetdetour.Models.Config;

public partial class WpfConfig
{
    [Obsolete("Use DefaultWebSocketAddress. Configuration files continue to accept Default_WebSocketAddress.")]
    public static string Default_WebSocketAddress
    {
        get => DefaultWebSocketAddress;
        set => DefaultWebSocketAddress = value;
    }

    [Obsolete("Use DefaultHttpAddress. Configuration files continue to accept Default_HttpAddress.")]
    public static string Default_HttpAddress
    {
        get => DefaultHttpAddress;
        set => DefaultHttpAddress = value;
    }

    [Obsolete("Use LastChatResponse. Configuration files continue to accept Get_Recv_String_ChatResult.")]
    public static string Get_Recv_String_ChatResult
    {
        get => LastChatResponse;
        set => LastChatResponse = value;
    }

    [Obsolete("Use SkipBedrockUpdates. Configuration files continue to accept IsBypassGameUpdate_Bedrock.")]
    public static bool IsBypassGameUpdate_Bedrock
    {
        get => SkipBedrockUpdates;
        set => SkipBedrockUpdates = value;
    }

    [Obsolete("Use EnableVerboseLogging. Configuration files continue to accept IsDebug.")]
    public static bool IsDebug
    {
        get => EnableVerboseLogging;
        set => EnableVerboseLogging = value;
    }

    [Obsolete("Use MacAddress. Configuration files continue to accept Mac_Addr.")]
    public static string Mac_Addr
    {
        get => MacAddress;
        set => MacAddress = value;
    }

    [Obsolete("Use RandomMacAddress. Configuration files continue to accept Random_Mac_Addr.")]
    public static string Random_Mac_Addr
    {
        get => RandomMacAddress;
        set => RandomMacAddress = value;
    }

    [Obsolete("Use Use64BitBedrock. Configuration files continue to accept IsEnableX64mc.")]
    public static bool IsEnableX64mc
    {
        get => Use64BitBedrock;
        set => Use64BitBedrock = value;
    }

    [Obsolete("Use ReceivedMessages. Configuration files continue to accept RecvList.")]
    public static JArray RecvList
    {
        get => ReceivedMessages;
        set => ReceivedMessages = value;
    }

    [Obsolete("Use IsLoggedIn. Configuration files continue to accept IsLogin.")]
    public static bool IsLogin
    {
        get => IsLoggedIn;
        set => IsLoggedIn = value;
    }

    [Obsolete("Use LauncherRootDirectory. Configuration files continue to accept wpflauncherRoot.")]
    public static string wpflauncherRoot
    {
        get => LauncherRootDirectory;
        set => LauncherRootDirectory = value;
    }

    [Obsolete("Use EnableWebServer. Configuration files continue to accept IsStartWebSocket.")]
    public static bool IsStartWebSocket
    {
        get => EnableWebServer;
        set => EnableWebServer = value;
    }

    [Obsolete("Use UseCustomServerAddress. Configuration files continue to accept IsCustomIP.")]
    public static bool IsCustomIP
    {
        get => UseCustomServerAddress;
        set => UseCustomServerAddress = value;
    }

    [Obsolete("Use HasSelectedServerAddress. Configuration files continue to accept IsSelectedIP.")]
    public static bool IsSelectedIP
    {
        get => HasSelectedServerAddress;
        set => HasSelectedServerAddress = value;
    }

    [Obsolete("Use SkipExitConfirmation. Configuration files continue to accept NoTwoExitMessage.")]
    public static bool NoTwoExitMessage
    {
        get => SkipExitConfirmation;
        set => SkipExitConfirmation = value;
    }

    [Obsolete("Use JoinRetryCount. Configuration files continue to accept JoinFailRetry.")]
    public static int JoinFailRetry
    {
        get => JoinRetryCount;
        set => JoinRetryCount = value;
    }

    [Obsolete("Use OptimizeMemoryBeforeLaunch. Configuration files continue to accept MemoryOptimize.")]
    public static bool MemoryOptimize
    {
        get => OptimizeMemoryBeforeLaunch;
        set => OptimizeMemoryBeforeLaunch = value;
    }

    [Obsolete("Use LogSensitiveAccountDetails. Configuration files continue to accept ShowAccountInfo.")]
    public static bool ShowAccountInfo
    {
        get => LogSensitiveAccountDetails;
        set => LogSensitiveAccountDetails = value;
    }

    [Obsolete("Use EnableChatNotifications. Configuration files continue to accept ShowWindowsNotify.")]
    public static bool ShowWindowsNotify
    {
        get => EnableChatNotifications;
        set => EnableChatNotifications = value;
    }

    [Obsolete("Use DownloadWorkerCount. Configuration files continue to accept MaxThread.")]
    public static int MaxThread
    {
        get => DownloadWorkerCount;
        set => DownloadWorkerCount = value;
    }

    [Obsolete("Use EnableParallelDownloads. Configuration files continue to accept IsDownloadMultiConfig.")]
    public static bool IsDownloadMultiConfig
    {
        get => EnableParallelDownloads;
        set => EnableParallelDownloads = value;
    }

    [Obsolete("Use ParallelDownloadThresholdMb. Configuration files continue to accept LimitDownload.")]
    public static int LimitDownload
    {
        get => ParallelDownloadThresholdMb;
        set => ParallelDownloadThresholdMb = value;
    }

    [Obsolete("Use BedrockDirectory. Configuration files continue to accept BedrockPath.")]
    public static string BedrockPath
    {
        get => BedrockDirectory;
        set => BedrockDirectory = value;
    }

    [Obsolete("Use KeepWindowsOnTop. Configuration files continue to accept IsWindowTopMost.")]
    public static bool IsWindowTopMost
    {
        get => KeepWindowsOnTop;
        set => KeepWindowsOnTop = value;
    }

    [Obsolete("Use EnableModInjection. Configuration files continue to accept EnableModsInject.")]
    public static bool EnableModsInject
    {
        get => EnableModInjection;
        set => EnableModInjection = value;
    }

    [Obsolete("Use WriteLauncherLogsToFile. Configuration files continue to accept IsLogOutputFolder.")]
    public static bool IsLogOutputFolder
    {
        get => WriteLauncherLogsToFile;
        set => WriteLauncherLogsToFile = value;
    }

    [Obsolete("Use EnableBedrockClientSelection. Configuration files continue to accept EnableCustomBedrockSelect.")]
    public static bool EnableCustomBedrockSelect
    {
        get => EnableBedrockClientSelection;
        set => EnableBedrockClientSelection = value;
    }

    [Obsolete("Use EnableAlternativeAccountLogin. Configuration files continue to accept EnableCustomAccountLogin.")]
    public static bool EnableCustomAccountLogin
    {
        get => EnableAlternativeAccountLogin;
        set => EnableAlternativeAccountLogin = value;
    }

    [Obsolete("Use UseAccountManagerLogin. Configuration files continue to accept MpayUnless.")]
    public static bool MpayUnless
    {
        get => UseAccountManagerLogin;
        set => UseAccountManagerLogin = value;
    }

    [Obsolete("Use EnableAdvancedSaveManager. Configuration files continue to accept AdvancedSavesManager.")]
    public static bool AdvancedSavesManager
    {
        get => EnableAdvancedSaveManager;
        set => EnableAdvancedSaveManager = value;
    }

    [Obsolete("Use ShowRoomDetailsWindow. Configuration files continue to accept ShowRoomManagerWindow.")]
    public static bool ShowRoomManagerWindow
    {
        get => ShowRoomDetailsWindow;
        set => ShowRoomDetailsWindow = value;
    }

    [Obsolete("Use PreserveResourcePacks. Configuration files continue to accept KeepOffDeleteLastResourcepacks.")]
    public static bool KeepOffDeleteLastResourcepacks
    {
        get => PreserveResourcePacks;
        set => PreserveResourcePacks = value;
    }

    [Obsolete("Use PreserveGameConfig. Configuration files continue to accept KeepOffDeleteLastConfig.")]
    public static bool KeepOffDeleteLastConfig
    {
        get => PreserveGameConfig;
        set => PreserveGameConfig = value;
    }

    [Obsolete("Use PreserveShaderPacks. Configuration files continue to accept KeepOffDeleteLastShaderPacks.")]
    public static bool KeepOffDeleteLastShaderPacks
    {
        get => PreserveShaderPacks;
        set => PreserveShaderPacks = value;
    }

    [Obsolete("Use EnablePortForwarding. Configuration files continue to accept AllowFrp.")]
    public static bool AllowFrp
    {
        get => EnablePortForwarding;
        set => EnablePortForwarding = value;
    }

    [Obsolete("Use EnableVirtualNetwork. Configuration files continue to accept UseNetworkMode.")]
    public static bool UseNetworkMode
    {
        get => EnableVirtualNetwork;
        set => EnableVirtualNetwork = value;
    }

    [Obsolete("Use CustomJvmArguments. Configuration files continue to accept CustomJVMArguments.")]
    public static string CustomJVMArguments
    {
        get => CustomJvmArguments;
        set => CustomJvmArguments = value;
    }

    [Obsolete("Use FriendStatuses. Configuration files continue to accept ListFriendStatus.")]
    public static List<FriendStatus> ListFriendStatus
    {
        get => FriendStatuses;
        set => FriendStatuses = value;
    }

    [Obsolete("Use CustomRecentServers. Configuration files continue to accept CustomRecentList.")]
    public static List<Tuple<string, NetGameResponse>> CustomRecentList
    {
        get => CustomRecentServers;
        set => CustomRecentServers = value;
    }

    [Obsolete("Use IsJoiningCustomServer. Configuration files continue to accept IsJoinCustomServer.")]
    public static bool IsJoinCustomServer
    {
        get => IsJoiningCustomServer;
        set => IsJoiningCustomServer = value;
    }

    [Obsolete("Use ShowCustomServers. Configuration files continue to accept ShowCustomServer.")]
    public static bool ShowCustomServer
    {
        get => ShowCustomServers;
        set => ShowCustomServers = value;
    }

    [Obsolete("Use LanNicknameFilterKeywords. Configuration files continue to accept LanGameNicknameFilterString.")]
    public static string LanGameNicknameFilterString
    {
        get => LanNicknameFilterKeywords;
        set => LanNicknameFilterKeywords = value;
    }

    [Obsolete("Use LanNicknameFilters. Configuration files continue to accept LanGameNicknameFilter.")]
    public static List<string> LanGameNicknameFilter
    {
        get => LanNicknameFilters;
        set => LanNicknameFilters = value;
    }

    [Obsolete("Use ShowGameLogsInConsole. Configuration files continue to accept ShowLogInConsole.")]
    public static bool ShowLogInConsole
    {
        get => ShowGameLogsInConsole;
        set => ShowGameLogsInConsole = value;
    }

    [Obsolete("Use ShowGameLogsWindow. Configuration files continue to accept ShowLogInWpf.")]
    public static bool ShowLogInWpf
    {
        get => ShowGameLogsWindow;
        set => ShowGameLogsWindow = value;
    }

    [Obsolete("Use SharedUidCipher. Configuration files continue to accept PublicSkip32Cipher.")]
    public static Skip32Cipher PublicSkip32Cipher
    {
        get => SharedUidCipher;
        set => SharedUidCipher = value;
    }
}