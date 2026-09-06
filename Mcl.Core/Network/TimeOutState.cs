using System.Net;

namespace Mcl.Core.Network;

public class TimeOutState
{
    public bool TimedOut { get; set; }

    public HttpWebRequest Request { get; set; }
}