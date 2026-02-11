using Microsoft.AspNetCore.Http;
using System.Net;

namespace ProjectBase.Core.Helpers;

/// <summary>
/// Extension methods for HttpContext to extract client IP address.
/// Handles proxy/load balancer scenarios by checking standard headers.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the client IP address from the HttpContext.
    /// Checks multiple headers in order: X-Forwarded-For, CF-Connecting-IP, X-Real-IP, then RemoteIpAddress.
    /// </summary>
    /// <param name="httpContext">The HttpContext instance.</param>
    /// <returns>The client IP address as a string, or null if not available.</returns>
    public static string? GetClientIpAddress(this HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        // X-Forwarded-For: Standard header for proxy/load balancer forwarding
        // Can contain multiple IPs (client, proxy1, proxy2), take the first one
        var xForwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xForwardedFor))
        {
            var ip = xForwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(ip) && IsValidIp(ip))
                return ip;
        }

        // CF-Connecting-IP: Cloudflare's header for the original client IP
        var cfConnectingIp = httpContext.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(cfConnectingIp) && IsValidIp(cfConnectingIp))
            return cfConnectingIp;

        // X-Real-IP: Alternative header used by some proxies (e.g., Nginx)
        var xRealIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xRealIp) && IsValidIp(xRealIp))
            return xRealIp;

        // Connection.RemoteIpAddress: Direct connection IP (fallback)
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            // IPv6 mapped IPv4 addresses (::ffff:192.168.1.1) should be converted to IPv4
            if (remoteIp.IsIPv4MappedToIPv6)
                return remoteIp.MapToIPv4().ToString();
            
            return remoteIp.ToString();
        }

        return null;
    }

    /// <summary>
    /// Basic validation to check if the string is a valid IP address format.
    /// </summary>
    private static bool IsValidIp(string ip)
    {
        return IPAddress.TryParse(ip, out _);
    }
}

