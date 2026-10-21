# Configuration

This document describes all configuration options available in Buzzard.

## Example

Please see [development example](../src/appsettings.Development.json).

## Configuration Files

Buzzard uses ASP.NET Core's configuration system with the following precedence:

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific, overwrites base)
3. Environment variables
4. Command line arguments

## Reverse Proxy Settings

Configure YARP reverse proxy in `appsettings.json`:

```json
"ReverseProxy": {
  "Routes": {
    "route1": {
      "ClusterId": "cluster1",
      "Match": { "Path": "{**catch-all}" }
    }
  },
  "Clusters": {
    "cluster1": {
      "Destinations": {
        "destination1": {
          "Address": "https://example.com/"
        }
      }
    }
  }
}
```

### Non-ASCII Request Headers

By default, YARP rejects request headers containing non-ASCII characters and returns `502 Bad Gateway`. For example, a request with `X-Test-Header: Tōkyō` fails with `HttpRequestException: Request headers must contain only ASCII characters.`

This affects deployments behind proxies or CDNs that add headers with non-ASCII values, such as Cloudflare's visitor location headers. To forward such headers, set `HttpClient.RequestHeaderEncoding` on each cluster:

```json
"Clusters": {
  "cluster1": {
    "HttpClient": { "RequestHeaderEncoding": "utf-8" },
    "Destinations": {
      "destination1": {
        "Address": "https://example.com/"
      }
    }
  }
}
```

**Note**: Make sure the backend accepts UTF-8 encoded header values.

See [YARP HTTP client configuration](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/http-client-config) for details.

## Client IP Settings

By default, Buzzard uses the IP address of the incoming connection as the client IP. Forwarded headers such as `X-Forwarded-For` are ignored because any client can send them.

When Buzzard runs behind other proxies (e.g. a CDN and a load balancer), configure which header contains the client IP and which proxies are trusted to set it:

```json
"ForwardedHeaders": {
  "ClientIpHeader": "X-Test-Client-Ip",
  "KnownProxies": ["10.0.0.1"],
  "KnownNetworks": ["10.0.0.0/8"],
  "DownstreamClientIpHeader": "X-Test-Downstream-Ip"
}
```

Configuration options:

- `ClientIpHeader`: Request header containing the client IP (e.g. `X-Forwarded-For`, or `CF-Connecting-IP` behind Cloudflare). If omitted, the connection IP is used.
- `KnownProxies`: IP addresses of proxies trusted to set `ClientIpHeader`. Optional.
- `KnownNetworks`: Networks in CIDR notation trusted to set `ClientIpHeader`. Optional.
- `DownstreamClientIpHeader`: Request header in which the client IP is sent to the destination. Any value sent by the client is replaced. If omitted, this header is not added.

When `KnownProxies` or `KnownNetworks` is configured, `ClientIpHeader` is used only when the connection comes from them. If neither is configured, `ClientIpHeader` is used for every connection and a warning is logged at startup. In this case, any client that can reach Buzzard directly can spoof its IP. When the header value is not a valid IP address, the connection IP is used. Invalid values in `KnownProxies` or `KnownNetworks` cause a startup error.

The client IP is used for request logs and the `X-Forwarded-For` header sent to the destination. YARP replaces any `X-Forwarded-For` sent by the client.

`ClientIpHeader` is forwarded to the destination with its original value. `X-Original-For` (the connection IP and port) is also added when the header is used. Because the client can send `ClientIpHeader` directly when it does not come through the trusted proxies, the destination should use `X-Forwarded-For` or `DownstreamClientIpHeader` instead.

**Note**: Trusting a header is only safe when clients cannot reach Buzzard without going through the trusted proxies. Restrict direct access at the network level.

See [Configure ASP.NET Core to work with proxy servers and load balancers](https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer) for details.

## Firewall Settings

Configure firewall rules to allow/deny requests:

```json
"Firewall": {
  "Path": {
    "Allow": {
      "Contains": ["/health", "/status"],
      "StartsWith": ["/api/public"],
      "EndsWith": [".css", ".js", ".png", ".ico"]
    },
    "Deny": {
      "Contains": ["/admin", "/config", "/.env"],
      "StartsWith": ["/private/", "/internal/"],
      "EndsWith": [".bak", ".tmp", ".log"]
    }
  },
  "UserAgent": {
    "Deny": {
      "Contains": [
        "bad"
      ],
      "StartsWith": [
        "evil/"
      ],
      "EndsWith": [
        "bot"
      ]
    },
    "ExemptPaths": ["/robots.txt"]
  }
}
```

### User-Agent Exempt Paths

`UserAgent.ExemptPaths` lists paths where User-Agent rules are not applied. For example, exempting `/robots.txt` lets denied bots read it and follow its crawling rules.

- Paths are matched exactly and case-sensitively. `/robots.txt` does not exempt `/ROBOTS.TXT`, `/robots.txt/`, `/robots.txt;x` or `/robots.txt/x`.
- The query string is not part of the path, so `/robots.txt?x=1` is also exempt.
- Path rules are still applied to exempt paths.

### Client Ban

Temporarily blocks clients that receive many error responses (e.g. scanners probing for files). Only error responses are counted, so pages with many assets do not affect legitimate users.

Optionally, it also blocks clients that send many page requests (e.g. crawlers fetching every article). See `RequestCount` below.

```json
"Firewall": {
  "ClientBan": {
    "Mode": "Observe",
    "CountedStatusCodes": [403, 404],
    "Threshold": 20,
    "WindowSeconds": 10,
    "BanSeconds": 600,
    "MaxTrackedClients": 10000,
    "GlobalSafetyThreshold": 10,
    "GlobalSafetyWindowSeconds": 60,
    "ExemptNetworks": ["192.0.2.0/24", "198.51.100.1"],
    "RequestCount": {
      "Enabled": false,
      "Methods": ["GET", "HEAD"],
      "Extensions": [""],
      "ExcludedQueryKeys": ["_rsc"]
    }
  }
}
```

Configuration options:

- `Mode`: `Disabled` (default), `Observe` (logs clients that would be banned without blocking them) or `Enforce` (blocks them).
- `CountedStatusCodes`: Response status codes counted as errors (default: `[403, 404]`). `403` responses returned by the firewall itself are also counted.
- `Threshold`: Number of errors within `WindowSeconds` that bans a client (default: `20`).
- `WindowSeconds`: Sliding window for counting errors (default: `10`).
- `BanSeconds`: Ban duration in seconds (default: `600`). The ban ends at a fixed time and is not extended by requests during the ban.
- `MaxTrackedClients`: Maximum number of tracked clients (default: `10000`). When exceeded, the least recently seen client that is not banned is discarded.
- `GlobalSafetyThreshold` / `GlobalSafetyWindowSeconds`: When more than `GlobalSafetyThreshold` clients exceed `Threshold` within `GlobalSafetyWindowSeconds` (defaults: `10` / `60`), Buzzard assumes an outage (e.g. the destination returning 404 for every page) rather than an attack and stops new bans until the count falls back. Existing bans are kept.
- `ExemptNetworks`: IP addresses or networks in CIDR notation that are never banned (e.g. monitoring services).
- `RequestCount`: Counts requests matching the conditions below, separately from errors. A client is banned when either count reaches `Threshold` within `WindowSeconds`. `BanSeconds`, `ExemptNetworks`, `Mode` and the safety mechanism are shared with error counting.
  - `Enabled`: Enables request counting (default: `false`).
  - `Methods`: HTTP methods to count, case-insensitive (default: `["GET", "HEAD"]`).
  - `Extensions`: File extensions of the last path segment to count, case-insensitive (default: `[""]`). `""` means no extension, so by default `/articles/a/`, `/articles/a` and `/` are counted while `/thumb.png` and `/feeds/index.xml` are not, and pages with many images do not affect legitimate users. A path ending with `/` is treated as having no extension. The leading `.` is optional (`"html"` and `".html"` are the same). Set `[]` to count requests regardless of extension.
  - `ExcludedQueryKeys`: Requests that have any of these query keys are not counted, case-insensitive (default: `[]`). For example, `["_rsc"]` excludes Next.js prefetch and client-side navigation requests.

Numeric options must be greater than `0`, otherwise Buzzard fails to start.

Behavior:

- Banned clients receive `403 Forbidden`.
- Responses are not counted when forwarding to the destination fails (e.g. `502` while the destination is down).
- The client IP is resolved as described in [Client IP Settings](#client-ip-settings). Configure it before enabling this feature behind a proxy, otherwise the proxy itself will be banned.
- State is kept in memory. It is lost on restart and is not shared between instances.

Logs:

| Level | Event |
|-------|-------|
| `Warning` | A client is banned (or would be banned in `Observe` mode), with the error or request count and recent paths |
| `Information` | A ban expires, with the number of requests received during the ban |
| `Warning` | A request is blocked by a ban, with the same fields as request logs and `Rule` set to `ClientBan` |
| `Warning` | The safety mechanism is activated or deactivated |

Expired bans are processed while handling requests, so the expiry log may be delayed until the next request.

Recommended rollout: run in `Observe` mode for a few days, confirm that legitimate users are not detected, adjust `Threshold` if needed, then switch to `Enforce`.

## Request Logging Settings

Each request is logged once. Blocked requests are logged at `Warning`, forwarding failures at `Error`, and other requests at `Information` after the response is produced.

```json
"RequestLogging": {
  "MaskedQueryKeys": ["token", "access_token", "api_key", "apikey", "key", "password", "secret"],
  "MaskedHeaders": ["Authorization", "Proxy-Authorization", "Cookie", "X-Api-Key", "X-Auth-Token"]
}
```

Configuration options:

- `MaskedQueryKeys`: Query parameter names whose values are replaced with `***` in logs (case-insensitive). If omitted, the keys shown above are used. Set `[]` to disable masking.
- `MaskedHeaders`: Request header names whose values are replaced with `***` in logs (case-insensitive). If omitted, the headers shown above are used. Set `[]` to disable masking.

Fields added to request logs:

| Field | Description |
|-------|-------------|
| `Host` | Request host |
| `QueryString` | Query string with masked values |
| `Rule` | Matched rule for blocked requests (e.g. `Path.Deny.Contains:/admin`) |
| `StatusCode` | Response status code |
| `ElapsedMs` | Processing time in milliseconds |
| `ForwarderError` | YARP forwarder error type when forwarding failed |
| `Header.<name>` | Every request header, with the name in lowercase (e.g. `Header.cf-ray`). Values of `MaskedHeaders` are replaced with `***` |

ASP.NET Core also adds `TraceId`, `SpanId`, `ParentId`, `RequestId`, `RequestPath` and `ConnectionId` to every log. `TraceId` follows the incoming `traceparent` header.

Path, User-Agent, query string and header values in logs are truncated to 1024 characters.

**Note**: Request headers may contain personal data (e.g. Cloudflare's `CF-IPCity`, `CF-IPLatitude`, `CF-IPLongitude`, `CF-Postal-Code`). Review your log retention policy accordingly.

## OpenTelemetry Settings

Buzzard supports OpenTelemetry for observability (tracing, metrics, and logs). Configure it in `appsettings.json`:

```json
"OpenTelemetry": {
  "ServiceName": "Buzzard",
  "ServiceNameSpace": "Buzzard",
  "ServiceVersion": "1.0.0",
  "Environment": "development",
  "OtlpEndpoint": "http://localhost:4317",
  "Headers": "Authorization=Basic {base64-encoded-credentials}"  // If you need
}
```

Configuration options:

- `ServiceName`: Name of your service (default: "buzzard")
- `ServiceNameSpace`: Service namespace for grouping (default: "")
- `ServiceVersion`: Version of your service (default: "1.0.0")
- `Environment`: Environment identifier (default: "development")
- `OtlpEndpoint`: OTLP collector endpoint URL (required - if not set, OpenTelemetry is disabled)
- `Headers`: Additional headers for authentication (default: "")

**Note**: If `OtlpEndpoint` is not configured, OpenTelemetry setup will be skipped and a message will be logged.

## Environment-Specific Configuration

### Development vs Production Settings

ASP.NET Core automatically loads environment-specific configuration files:

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific, overwrites base)
3. Environment variables
4. Command line arguments

### Running in Different Environments

#### Development (default):

```bash
dotnet run
# Loads: appsettings.json + appsettings.Development.json
```

#### Production:

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run
# Loads: appsettings.json + appsettings.Production.json
```

## Port Configuration

### Development

Port configuration is handled by `Properties/launchSettings.json` (Visual Studio/dotnet run only).

### Production

`launchSettings.json` is **not used in production**. Configure ports using:

#### 1. Environment variables (recommended):

```bash
export ASPNETCORE_URLS="https://+:443;http://+:80"
dotnet Buzzard.dll
```

#### 2. Command line arguments:

```bash
dotnet Buzzard.dll --urls "https://+:443;http://+:80"
```

#### 3. appsettings.Production.json:

```json
{
    "Kestrel": {
    "Endpoints": {
        "Http": { "Url": "http://+:80" },
        "Https": { "Url": "https://+:443" }
    }
    }
}
```

## Configuration Changes

- Configuration changes in `appsettings.json` require server restart
- No rebuild needed for configuration changes
- Configuration files are not hot-reloaded in ASP.NET Core by default
