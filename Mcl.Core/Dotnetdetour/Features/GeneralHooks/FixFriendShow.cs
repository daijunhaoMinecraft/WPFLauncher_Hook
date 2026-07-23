using System.Collections.Generic;
using System.Linq;
using Mcl.Core.Dotnetdetour.CoreEngine.Attributes;
using Mcl.Core.Dotnetdetour.CoreEngine.Interfaces;
using Mcl.Core.Dotnetdetour.Models.Config;
using WPFLauncher.Model;

namespace Mcl.Core.Dotnetdetour.Features.GeneralHooks;

// 修复本地联机好友显示问题
public class FixLanGameFriendShow : IMethodHook
{
    [OriginalMethod]
    public List<UserM> OriginalFriendList()
    {
        return new List<UserM>();
    }

    [HookMethod("WPFLauncher.Model.aho", "h")]
    public List<UserM> GetFriendList()
    {
        var friendList = OriginalFriendList();
        foreach (var friend in friendList)
        {
            var searchedFriend =
                WpfConfig.ListFriendStatus.FirstOrDefault(x => x.UserId.ToString() == friend.UserID.ToString());
            if (searchedFriend != null)
            {
                var isOnline = searchedFriend.Status == 1;
                friend.Status = isOnline ? OnLineState.ONLINE : OnLineState.OFFLINE;
            }
        }

        return friendList;
    }
}