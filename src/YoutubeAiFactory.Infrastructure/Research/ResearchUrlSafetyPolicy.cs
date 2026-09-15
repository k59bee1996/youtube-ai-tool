using System.Net;
using System.Net.Sockets;

namespace YoutubeAiFactory.Infrastructure.Research;

/// <summary>DNS-aware public-web boundary for server-side source retrieval, applied on every redirect.</summary>
internal sealed class ResearchUrlSafetyPolicy
{
    public static async Task<bool> IsAllowedAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (!uri.IsAbsoluteUri || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            uri.UserInfo.Length > 0) return false;
        return await ResolvePublicAddressAsync(uri.DnsSafeHost, cancellationToken) is not null;
    }

    /// <summary>Connects only to the public address that passed the DNS boundary check.</summary>
    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var address = await ResolvePublicAddressAsync(context.DnsEndPoint.Host, cancellationToken)
            ?? throw new HttpRequestException("Source host did not resolve to a public address.");
        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static async Task<IPAddress?> ResolvePublicAddressAsync(string host, CancellationToken cancellationToken)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase)) return null;
        if (IPAddress.TryParse(host, out var literal)) return IsGloballyRoutable(literal) ? literal : null;
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            return addresses.Length > 0 && addresses.All(IsGloballyRoutable) ? addresses[0] : null;
        }
        catch (SocketException) { return null; }
    }

    private static bool IsGloballyRoutable(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) return IsGloballyRoutable(address.MapToIPv4());
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6None)) return false;
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var first = bytes[0];
            var second = bytes[1];
            var third = bytes[2];
            if (first is 0 or 10 or 127 || first >= 224) return false;
            if (first == 100 && second is >= 64 and <= 127) return false;
            if (first == 169 && second == 254) return false;
            if (first == 172 && second is >= 16 and <= 31) return false;
            if (first == 192 && (second == 0 || second == 2 || second == 168 || (second == 88 && third == 99))) return false;
            if (first == 198 && (second is 18 or 19 || (second == 51 && third == 100))) return false;
            if (first == 203 && second == 0 && third == 113) return false;
            return true;
        }
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast) return false;
            if ((bytes[0] & 0xFE) == 0xFC || IsZeroPrefix(bytes, 12) || IsInPrefix(bytes, [0x01, 0x00, 0, 0, 0, 0, 0, 0], 64) ||
                IsInPrefix(bytes, [0x20, 0x01, 0x00, 0x00], 23) || IsInPrefix(bytes, [0x20, 0x01, 0x0D, 0xB8], 32)) return false;
            return true;
        }
        return false;
    }

    private static bool IsZeroPrefix(byte[] bytes, int length) => bytes.Take(length).All(value => value == 0);

    private static bool IsInPrefix(byte[] bytes, byte[] prefix, int prefixLength)
    {
        var fullBytes = prefixLength / 8;
        if (!bytes.AsSpan(0, fullBytes).SequenceEqual(prefix.AsSpan(0, fullBytes))) return false;
        var remainder = prefixLength % 8;
        return remainder == 0 || (bytes[fullBytes] & (0xFF << (8 - remainder))) == (prefix[fullBytes] & (0xFF << (8 - remainder)));
    }
}
