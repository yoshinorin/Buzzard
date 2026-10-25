using Buzzard.Models;
using Buzzard.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Buzzard.Tests;

public class ClientBanStoreTests
{
    private readonly FakeTimeProvider _timeProvider = new();

    [Fact]
    public void ExceedingMaxTrackedClients_EvictsLeastRecentlySeenClient()
    {
        var store = CreateStore(config => config.MaxTrackedClients = 2);
        RecordErrors(store, "203.0.113.1", 2);
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        RecordErrors(store, "203.0.113.2", 1);
        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        RecordErrors(store, "203.0.113.3", 1);

        Assert.Equal(2, store.TrackedClientCount);
        RecordErrors(store, "203.0.113.1", 1);
        Assert.False(store.IsBanned("203.0.113.1"));
    }

    [Fact]
    public void ExceedingMaxTrackedClients_DoesNotEvictBannedClients()
    {
        var store = CreateStore(config => config.MaxTrackedClients = 1);
        RecordErrors(store, "203.0.113.1", 3);
        RecordErrors(store, "203.0.113.2", 3);

        Assert.Equal(1, store.TrackedClientCount);
        Assert.True(store.IsBanned("203.0.113.1"));
        Assert.False(store.IsBanned("203.0.113.2"));
    }

    [Fact]
    public void Cleanup_RemovesClientsWithoutErrorsInWindow()
    {
        var store = CreateStore();
        RecordErrors(store, "203.0.113.1", 1);

        _timeProvider.Advance(TimeSpan.FromSeconds(10));
        store.IsBanned("198.51.100.20");

        Assert.Equal(0, store.TrackedClientCount);
    }

    [Fact]
    public void Cleanup_RemovesClientsWithoutRequestsInWindow()
    {
        var store = CreateStore();
        store.RecordRequest("203.0.113.1", "/test");

        _timeProvider.Advance(TimeSpan.FromSeconds(9));
        store.IsBanned("198.51.100.20");
        Assert.Equal(1, store.TrackedClientCount);

        _timeProvider.Advance(TimeSpan.FromSeconds(1));
        store.IsBanned("198.51.100.20");
        Assert.Equal(0, store.TrackedClientCount);
    }

    [Fact]
    public void Cleanup_RemovesClientsWithExpiredBans()
    {
        var store = CreateStore();
        RecordErrors(store, "203.0.113.1", 3);

        _timeProvider.Advance(TimeSpan.FromSeconds(599));
        store.IsBanned("198.51.100.20");
        Assert.Equal(1, store.TrackedClientCount);

        _timeProvider.Advance(TimeSpan.FromSeconds(10));
        store.IsBanned("198.51.100.20");
        Assert.Equal(0, store.TrackedClientCount);
    }

    [Fact]
    public void BanExpiry_LogsBlockedRequestCount()
    {
        var logger = new Mock<ILogger<ClientBanStore>>();
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var store = CreateStore(logger: logger.Object);
        RecordErrors(store, "203.0.113.1", 3);
        store.IsBanned("203.0.113.1");
        store.IsBanned("203.0.113.1");

        _timeProvider.Advance(TimeSpan.FromSeconds(600));
        store.IsBanned("203.0.113.1");

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString() == "Client ban expired (Enforce): 203.0.113.1 2 requests during the ban"),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(false, "3 errors in 10s")]
    [InlineData(true, "3 requests in 10s")]
    public void BanStart_LogsCountType(bool recordRequests, string expected)
    {
        var logger = new Mock<ILogger<ClientBanStore>>();
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var store = CreateStore(logger: logger.Object);
        for (var i = 0; i < 3; i++)
        {
            if (recordRequests)
            {
                store.RecordRequest("203.0.113.1", "/test");
            }
            else
            {
                store.RecordError("203.0.113.1", "/test");
            }
        }

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.StartsWith($"Client ban started (Enforce): 203.0.113.1 {expected},")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(100, true)]
    public void RecordError_CountsWeight(int weight, bool banned)
    {
        var store = CreateStore();
        store.RecordError("203.0.113.1", "/test", weight);

        Assert.Equal(banned, store.IsBanned("203.0.113.1"));
    }

    private ClientBanStore CreateStore(Action<ClientBanConfig>? configure = null, ILogger<ClientBanStore>? logger = null)
    {
        var config = new ClientBanConfig
        {
            Mode = ClientBanMode.Enforce,
            Threshold = 3,
            WindowSeconds = 10,
            BanSeconds = 600,
        };
        configure?.Invoke(config);
        return new ClientBanStore(Options.Create(config), _timeProvider, logger ?? NullLogger<ClientBanStore>.Instance);
    }

    private static void RecordErrors(ClientBanStore store, string clientIp, int count)
    {
        for (var i = 0; i < count; i++)
        {
            store.RecordError(clientIp, "/test");
        }
    }
}
