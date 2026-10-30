using System;
using System.Globalization;
using System.Linq;
using Mcl.Core.Dotnetdetour.Models.Config;
using Mcl.Core.Dotnetdetour.Models.Entities;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

internal static class FriendStatusStore
{
    private static readonly object Sync = new();

    public static void SetPresence(uint userId, bool isOnline)
        => SetPresence(userId.ToString(CultureInfo.InvariantCulture), isOnline);

    public static void SetPresence(string userId, bool isOnline)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var normalizedUserId = userId.Trim();
        lock (Sync)
        {
            var status = WpfConfig.FriendStatuses.FirstOrDefault(x =>
                string.Equals(x.UserId, normalizedUserId, StringComparison.Ordinal));
            if (status == null)
            {
                WpfConfig.FriendStatuses.Add(new FriendStatus
                {
                    UserId = normalizedUserId,
                    Status = isOnline ? 1 : 0
                });
                return;
            }

            status.Status = isOnline ? 1 : 0;
        }
    }

    public static bool TryGetOnline(uint userId, out bool isOnline)
        => TryGetOnline(userId.ToString(CultureInfo.InvariantCulture), out isOnline);

    public static bool TryGetOnline(string userId, out bool isOnline)
    {
        isOnline = false;
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var normalizedUserId = userId.Trim();
        lock (Sync)
        {
            var status = WpfConfig.FriendStatuses.FirstOrDefault(x =>
                string.Equals(x.UserId, normalizedUserId, StringComparison.Ordinal));
            if (status == null) return false;

            isOnline = status.Status != 0;
            return true;
        }
    }
}
