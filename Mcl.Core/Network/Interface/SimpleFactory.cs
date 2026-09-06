namespace Mcl.Core.Network.Interface;

public class SimpleFactory<T> : IHttpFactory where T : IHttp, new()
{
    public IHttp Create()
    {
        return new T();
    }
}