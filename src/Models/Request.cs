namespace Buzzard.Models;

public class Request
{
    public string Ip { get; }
    public string Method { get; }
    public string Host { get; }
    public string Path { get; }
    public string UserAgent { get; }
    public string QueryString { get; }

    public Request(string ip, string method, string host, string path, string userAgent, string queryString)
    {
        Ip = ip;
        Method = method;
        Host = host;
        Path = path;
        QueryString = queryString;
        UserAgent = userAgent;
    }
}
