namespace Buzzard.Services;

public interface IClientBanStore
{
    bool IsBanned(string clientIp);
    void RecordError(string clientIp, string path);
    void RecordRequest(string clientIp, string path);
}
