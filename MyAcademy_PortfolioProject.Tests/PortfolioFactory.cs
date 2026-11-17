using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MyAcademy_PortfolioProject.Context;

namespace MyAcademy_PortfolioProject.Tests;

public sealed class PortfolioFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public SaveFailureInterceptor SaveFailures { get; } = new();
    private readonly string environment;
    private readonly IDictionary<string,string?>? settings;
    public PortfolioFactory(string environment="Testing",IDictionary<string,string?>? settings=null)
    {
        this.environment=environment; this.settings=settings; connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        if(settings!=null) builder.ConfigureAppConfiguration((_,configuration)=>configuration.AddInMemoryCollection(settings));
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PortfolioContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<PortfolioContext>>();
            services.AddDbContext<PortfolioContext>(options => options.UseSqlite(connection).AddInterceptors(SaveFailures));
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }

    public HttpClient NewClient()
    {
        var client = CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PortfolioContext>().Database.EnsureCreated();
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}
