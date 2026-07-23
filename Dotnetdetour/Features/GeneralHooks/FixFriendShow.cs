using System.Collections.Generic;
using System.Linq;
using Mcl.Core.Dotnetdetour.Model;
using WPFLauncher.Model;

namespace Mcl.Core.Dotnetdetour.HookList;

// 修复本地联机好友显示问题
public class FixLanGameFriendShow : IMethodHook
{
    [OriginalMethod]
    public List<UserM> OriginalFriendList()
    {
        return new List<UserM>();
    }
    [HookMethod("WPFLauncher.Model.aho", "h", null)]
    public List<UserM> GetFriendList()
    {
        List<UserM> friendList = OriginalFriendList();
        foreach (UserM friend in friendList)
        {
            FriendStatus searchedFriend = WpfConfig.ListFriendStatus.FirstOrDefault(x => x.UserId.ToString() == friend.UserID.ToString());
            if (searchedFriend != null)
            {
                bool isOnline = searchedFriend.Status == 1;
                friend.Status = isOnline ? OnLineState.ONLINE : OnLineState.OFFLINE;
            }
        }
        return friendList;
    }
}