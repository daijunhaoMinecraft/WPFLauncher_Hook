namespace Mcl.Core.Network.Interface;

public interface INetResponse<T> : INetResponse
{
    T Data { get; set; }
}