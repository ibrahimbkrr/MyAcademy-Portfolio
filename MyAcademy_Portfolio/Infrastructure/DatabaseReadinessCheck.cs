using Microsoft.Extensions.Diagnostics.HealthChecks;
using MyAcademy_PortfolioProject.Context;

namespace MyAcademy_PortfolioProject.Infrastructure;

public sealed class DatabaseReadinessCheck(PortfolioContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken=default)
    {
        try { return await db.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy(); }
        catch { return HealthCheckResult.Unhealthy(); }
    }
}
