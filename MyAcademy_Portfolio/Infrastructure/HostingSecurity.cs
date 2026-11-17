using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace MyAcademy_PortfolioProject.Infrastructure;

public static class HostingSecurity
{
    public const string ContactPolicy = "public-contact";

    public static IServiceCollection AddHostingSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(ContactPolicy, context => RateLimitPartition.GetSlidingWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown-peer",
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5, Window = TimeSpan.FromMinutes(10), SegmentsPerWindow = 10,
                    QueueLimit = 0, AutoReplenishment = true
                }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter = "600";
                context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
                await context.HttpContext.Response.WriteAsync("""
                    <!doctype html><html lang="tr"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
                    <title>Lütfen biraz bekleyin</title><body style="background:#f8f7f2;color:#244d40;font-family:system-ui;padding:2rem">
                    <main><h1>Lütfen biraz bekleyin</h1><p>Kısa sürede çok fazla mesaj gönderildi. 10 dakika sonra yeniden deneyebilirsiniz.</p>
                    <a href="/#contact">İletişim formuna dön</a></main></body></html>
                    """, cancellationToken);
            };
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            // Middleware is enabled only when at least one exact proxy IP is explicitly configured.
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var value in configuration.GetSection("ReverseProxy:TrustedProxies").Get<string[]>() ?? [])
                options.KnownProxies.Add(IPAddress.Parse(value));
        });

        var protection = services.AddDataProtection().SetApplicationName("MyAcademy.Portfolio");
        if (configuration["DataProtection:KeyDirectory"] is { Length: > 0 } keyDirectory)
            protection.PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(keyDirectory)));
        services.AddHealthChecks();
        return services;
    }
}
