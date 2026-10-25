namespace Buzzard.Models;

public enum ClientBanMode
{
    Disabled,
    Observe,
    Enforce,
}

public class ClientBanConfig
{
    public static readonly int[] DefaultCountedStatusCodes = [403, 404];

    public ClientBanMode Mode { get; set; } = ClientBanMode.Disabled;
    public int[]? CountedStatusCodes { get; set; }
    public int Threshold { get; set; } = 20;
    public int WindowSeconds { get; set; } = 10;
    public int BanSeconds { get; set; } = 600;
    public int MaxTrackedClients { get; set; } = 10000;
    public int GlobalSafetyThreshold { get; set; } = 10;
    public int GlobalSafetyWindowSeconds { get; set; } = 60;
    public string[] ExemptNetworks { get; set; } = [];
    public int IPv4Prefix { get; set; } = 32;
    public int IPv6Prefix { get; set; } = 64;
    public ClientBanRequestCountConfig RequestCount { get; set; } = new();
}

public class ClientBanRequestCountConfig
{
    public static readonly string[] DefaultMethods = ["GET", "HEAD"];
    public static readonly string[] DefaultExtensions = [""];

    public bool Enabled { get; set; }
    public string[]? Methods { get; set; }
    public string[]? Extensions { get; set; }
    public string[] ExcludedQueryKeys { get; set; } = [];
}
