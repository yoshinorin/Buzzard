using Buzzard.Models;
using Microsoft.Extensions.Options;

namespace Buzzard.Services;

public class ClientBanStore : IClientBanStore
{
    private const int MaxRecentPaths = 5;
    private const int MaxPathLength = 256;

    private readonly ClientBanConfig _config;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ClientBanStore> _logger;
    private readonly TimeSpan _window;
    private readonly TimeSpan _banDuration;
    private readonly TimeSpan _globalSafetyWindow;
    private readonly object _lock = new();
    private readonly Dictionary<string, ClientEntry> _clients = [];
    private readonly Dictionary<string, DateTimeOffset> _thresholdExceeded = [];
    private DateTimeOffset _lastCleanup;
    private bool _safetyActive;

    public ClientBanStore(IOptions<ClientBanConfig> config, TimeProvider timeProvider, ILogger<ClientBanStore> logger)
    {
        _config = config.Value;
        _timeProvider = timeProvider;
        _logger = logger;
        _window = TimeSpan.FromSeconds(_config.WindowSeconds);
        _banDuration = TimeSpan.FromSeconds(_config.BanSeconds);
        _globalSafetyWindow = TimeSpan.FromSeconds(_config.GlobalSafetyWindowSeconds);
        _lastCleanup = timeProvider.GetUtcNow();
    }

    public int TrackedClientCount
    {
        get
        {
            lock (_lock)
            {
                return _clients.Count;
            }
        }
    }

    public bool IsBanned(string clientIp)
    {
        lock (_lock)
        {
            var now = _timeProvider.GetUtcNow();
            CleanupIfDue(now);

            if (!_clients.TryGetValue(clientIp, out var entry))
            {
                return false;
            }

            ExpireBanIfDue(clientIp, entry, now);
            if (!entry.IsBanned(now))
            {
                return false;
            }

            entry.BlockedCount++;
            return true;
        }
    }

    public void RecordError(string clientIp, string path)
    {
        Record(clientIp, path, entry => entry.Errors, "errors");
    }

    public void RecordRequest(string clientIp, string path)
    {
        Record(clientIp, path, entry => entry.Requests, "requests");
    }

    private void Record(string clientIp, string path, Func<ClientEntry, Queue<DateTimeOffset>> selectCounter, string countType)
    {
        lock (_lock)
        {
            var now = _timeProvider.GetUtcNow();
            CleanupIfDue(now);

            if (!_clients.TryGetValue(clientIp, out var entry))
            {
                if (!MakeRoomForNewClient())
                {
                    return;
                }
                entry = new ClientEntry();
                _clients[clientIp] = entry;
            }

            ExpireBanIfDue(clientIp, entry, now);
            if (entry.IsBanned(now))
            {
                return;
            }

            var counter = selectCounter(entry);
            entry.LastSeen = now;
            counter.Enqueue(now);
            entry.RecentPaths.Enqueue(path.Length <= MaxPathLength ? path : path[..MaxPathLength]);
            if (entry.RecentPaths.Count > MaxRecentPaths)
            {
                entry.RecentPaths.Dequeue();
            }
            entry.RemoveCountsBefore(now - _window);

            if (counter.Count < _config.Threshold)
            {
                return;
            }

            _thresholdExceeded[clientIp] = now;
            UpdateSafety(now);
            if (_safetyActive)
            {
                return;
            }

            entry.BannedUntil = now + _banDuration;
            _logger.LogWarning(
                "Client ban started ({Mode}): {Ip} {Count} {CountType} in {WindowSeconds}s, banned until {BannedUntil}, recent paths: {RecentPaths}",
                _config.Mode, clientIp, counter.Count, countType, _config.WindowSeconds, entry.BannedUntil, string.Join(", ", entry.RecentPaths));
            entry.Errors.Clear();
            entry.Requests.Clear();
        }
    }

    private void CleanupIfDue(DateTimeOffset now)
    {
        if (now - _lastCleanup < _window)
        {
            return;
        }
        _lastCleanup = now;

        foreach (var (clientIp, entry) in _clients.ToList())
        {
            ExpireBanIfDue(clientIp, entry, now);
            entry.RemoveCountsBefore(now - _window);
            if (entry.BannedUntil is null && entry.Errors.Count == 0 && entry.Requests.Count == 0)
            {
                _clients.Remove(clientIp);
            }
        }

        UpdateSafety(now);
    }

    private void ExpireBanIfDue(string clientIp, ClientEntry entry, DateTimeOffset now)
    {
        if (entry.BannedUntil is not { } bannedUntil || bannedUntil > now)
        {
            return;
        }

        _logger.LogInformation("Client ban expired ({Mode}): {Ip} {BlockedCount} requests during the ban", _config.Mode, clientIp, entry.BlockedCount);
        entry.BannedUntil = null;
        entry.BlockedCount = 0;
    }

    private void UpdateSafety(DateTimeOffset now)
    {
        foreach (var (clientIp, exceededAt) in _thresholdExceeded.ToList())
        {
            if (exceededAt <= now - _globalSafetyWindow)
            {
                _thresholdExceeded.Remove(clientIp);
            }
        }

        var active = _thresholdExceeded.Count > _config.GlobalSafetyThreshold;
        if (active == _safetyActive)
        {
            return;
        }

        _safetyActive = active;
        if (active)
        {
            _logger.LogWarning(
                "Client ban safety activated: {ClientCount} clients exceeded the threshold in {GlobalSafetyWindowSeconds}s. New bans are suspended",
                _thresholdExceeded.Count, _config.GlobalSafetyWindowSeconds);
        }
        else
        {
            _logger.LogWarning("Client ban safety deactivated. New bans are resumed");
        }
    }

    private bool MakeRoomForNewClient()
    {
        if (_clients.Count < _config.MaxTrackedClients)
        {
            return true;
        }

        // Banned clients are never evicted so that a flood of new clients cannot lift bans.
        var oldest = _clients
            .Where(c => c.Value.BannedUntil is null)
            .OrderBy(c => c.Value.LastSeen)
            .Select(c => c.Key)
            .FirstOrDefault();
        if (oldest is null)
        {
            return false;
        }

        _clients.Remove(oldest);
        return true;
    }

    private sealed class ClientEntry
    {
        public Queue<DateTimeOffset> Errors { get; } = new();
        public Queue<DateTimeOffset> Requests { get; } = new();
        public Queue<string> RecentPaths { get; } = new();
        public DateTimeOffset LastSeen { get; set; }
        public DateTimeOffset? BannedUntil { get; set; }
        public int BlockedCount { get; set; }

        public bool IsBanned(DateTimeOffset now) => BannedUntil > now;

        public void RemoveCountsBefore(DateTimeOffset threshold)
        {
            RemoveBefore(Errors, threshold);
            RemoveBefore(Requests, threshold);
        }

        private static void RemoveBefore(Queue<DateTimeOffset> counter, DateTimeOffset threshold)
        {
            while (counter.TryPeek(out var oldest) && oldest <= threshold)
            {
                counter.Dequeue();
            }
        }
    }
}
