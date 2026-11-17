using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Tests;

public sealed class RestoredDatabaseFactAttribute : FactAttribute
{
    public RestoredDatabaseFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PORTFOLIO_REHEARSAL_EVIDENCE")))
            Skip="Requires verified backup/restore evidence and explicitly isolated restored catalog.";
    }
}

public sealed class RestoredDatabaseTests
{
    private sealed class RestoredFactory(string catalog) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if(!Regex.IsMatch(catalog,@"^PortfolioRehearsal_\d{8}_\d{6}_[a-f0-9]{32}$"))
                throw new InvalidOperationException("Only a uniquely named restored rehearsal catalog is permitted");
            var config=new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(Environment.GetEnvironmentVariable("PORTFOLIO_SQL_TEST_CONFIG")!)).Build();
            var connection=new SqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection"));
            if(string.IsNullOrWhiteSpace(connection.DataSource) || connection.InitialCatalog==catalog)
                throw new InvalidOperationException("Explicit source configuration and a separate restored catalog are required");
            connection.InitialCatalog=catalog;
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging=>logging.ClearProviders());
            builder.ConfigureServices(services=> {
                services.RemoveAll<DbContextOptions<PortfolioContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<PortfolioContext>>();
                services.AddDbContext<PortfolioContext>(options=>options.UseSqlServer(connection.ConnectionString));
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
            });
        }
    }

    [RestoredDatabaseFact]
    public async Task Restored_copy_application_regression()
    {
        var evidence=Path.GetFullPath(Environment.GetEnvironmentVariable("PORTFOLIO_REHEARSAL_EVIDENCE")!);
        using var run=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(evidence,"run.json")));
        Assert.True(run.RootElement.GetProperty("BackupVerified").GetBoolean());
        Assert.True(run.RootElement.GetProperty("RestoreVerified").GetBoolean());
        var catalog=run.RootElement.GetProperty("RestoredDatabase").GetString()!;
        using var stage=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(evidence,"staging-success.json")));
        Assert.Equal(catalog,stage.RootElement.GetProperty("Catalog").GetString());
        using var factory=new RestoredFactory(catalog);
        using var http=factory.CreateClient(new() {BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        using var scope=factory.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        var categories=await db.Categories.AsNoTracking().OrderBy(c=>c.CategoryId).Select(c=>new {c.CategoryId,c.CategoryName,c.FilterKey}).ToListAsync();
        var relationships=await db.Projects.AsNoTracking().OrderBy(p=>p.ProjectId).Select(p=>new {p.ProjectId,p.CategoryId}).ToListAsync();
        Assert.Equal("Test Otomasyonu (Arşiv)",categories.Single(c=>c.CategoryId==3).CategoryName);
        Assert.Equal("Test Otomasyonu",categories.Single(c=>c.CategoryId==7).CategoryName);
        Assert.Equal(HttpStatusCode.OK,(await http.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await http.GetAsync("/Admin/Account/Login")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await http.GetAsync("/Admin/Category/CategoryList")).StatusCode);

        async Task<string> Token(string path)
        {
            var html=await http.GetStringAsync(path);
            var match=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Assert.True(match.Success);return WebUtility.HtmlDecode(match.Groups[1].Value);
        }
        async Task<HttpResponseMessage> Post(string path,Dictionary<string,string> fields,string? tokenPath=null)
        {
            fields["__RequestVerificationToken"]=await Token(tokenPath??path);
            return await http.PostAsync(path,new FormUrlEncodedContent(fields));
        }

        var users=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True(await roles.RoleExistsAsync("Admin")); // Existing role is never reset or changed.
        var email="rehearsal-"+Guid.NewGuid().ToString("N")+"@example.test";
        var password="Aa1!"+Guid.NewGuid().ToString("N");
        var auditUser=new AppUser {UserName=email,Email=email,EmailConfirmed=true};
        Assert.True((await users.CreateAsync(auditUser,password)).Succeeded);
        var original=categories.Single(c=>c.CategoryId==4);
        var changed=false;
        try
        {
            Assert.True((await users.AddToRoleAsync(auditUser,"Admin")).Succeeded);
            Assert.Equal(HttpStatusCode.Redirect,(await Post("/Admin/Account/Login",new() {{"Email",email},{"Password",password}})).StatusCode);
            foreach(var path in new[]{"/Admin/Category/CategoryList","/Admin/Category/UpdateCategory/3","/Admin/Project/ProjectList","/Admin/Dashboard/Index"})
                Assert.Equal(HttpStatusCode.OK,(await http.GetAsync(path)).StatusCode);
            var html=WebUtility.HtmlDecode(await http.GetStringAsync("/Admin/Category/CategoryList"));
            Assert.Contains("Test Otomasyonu (Arşiv)",html);Assert.Contains("Test Otomasyonu",html);
            var temporary="Rehearsal "+Guid.NewGuid().ToString("N");
            var update=await Post("/Admin/Category/UpdateCategory",new() {{"CategoryId","4"},{"CategoryName",temporary},{"FilterKey",original.FilterKey!}},"/Admin/Category/UpdateCategory/4");
            changed=true;
            Assert.Equal(HttpStatusCode.Redirect,update.StatusCode);
            Assert.Equal(temporary,await db.Categories.AsNoTracking().Where(c=>c.CategoryId==4).Select(c=>c.CategoryName).SingleAsync());
            Assert.Equal(HttpStatusCode.Redirect,(await Post("/Admin/Category/UpdateCategory",new() {{"CategoryId","4"},{"CategoryName",original.CategoryName},{"FilterKey",original.FilterKey!}},"/Admin/Category/UpdateCategory/4")).StatusCode);
            changed=false;
            Assert.Equal(categories,await db.Categories.AsNoTracking().OrderBy(c=>c.CategoryId).Select(c=>new {c.CategoryId,c.CategoryName,c.FilterKey}).ToListAsync());
            Assert.Equal(relationships,await db.Projects.AsNoTracking().OrderBy(p=>p.ProjectId).Select(p=>new {p.ProjectId,p.CategoryId}).ToListAsync());
            Assert.Equal(HttpStatusCode.Redirect,(await Post("/Admin/Account/Logout",new(),"/Admin/Dashboard/Index")).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect,(await http.GetAsync("/Admin/Category/CategoryList")).StatusCode);

            using var playwright=await Playwright.CreateAsync();
            await using var browser=await playwright.Chromium.LaunchAsync(new() {Channel="msedge",Headless=true});
            var page=await browser.NewPageAsync();
            var errors=new List<string>();page.PageError+=(_,error)=>errors.Add(error);
            await page.RouteAsync("https://localhost/**",async route=> {
                // Only GET requests are allowed in this public browser transport.
                Assert.Equal("GET",route.Request.Method);
                using var response=await http.GetAsync(route.Request.Url);
                await route.FulfillAsync(new() {Status=(int)response.StatusCode,BodyBytes=await response.Content.ReadAsByteArrayAsync(),ContentType=response.Content.Headers.ContentType?.ToString()??"text/html"});
            });
            await page.GotoAsync("https://localhost/");
            var active=await db.Projects.CountAsync(p=>p.IsActive);
            var automation=await db.Projects.CountAsync(p=>p.IsActive&&p.CategoryId==7);
            await Assertions.Expect(page.Locator(".project-item")).ToHaveCountAsync(active);
            await page.Locator("#projectFilters [data-filter=automation]").ClickAsync();
            await Assertions.Expect(page.Locator(".project-item:visible")).ToHaveCountAsync(automation);
            await Assertions.Expect(page.Locator(".project-item:visible .project-category-tag")).ToHaveTextAsync("Test Otomasyonu");
            await page.Locator("#projectFilters [data-filter=all]").ClickAsync();
            await Assertions.Expect(page.Locator(".project-item:visible")).ToHaveCountAsync(active);
            Assert.Empty(errors);
            await page.ScreenshotAsync(new() {Path=Path.Combine(evidence,"restored-public.png"),FullPage=true});
        }
        finally
        {
            // Cleanup only our own isolated credential and reversible test rename, never original Identity users.
            if(changed) await db.Categories.Where(c=>c.CategoryId==4).ExecuteUpdateAsync(s=>s.SetProperty(c=>c.CategoryName,original.CategoryName));
            Assert.True((await users.DeleteAsync(auditUser)).Succeeded);
        }
        await File.WriteAllTextAsync(Path.Combine(evidence,"restored-app-success.json"),JsonSerializer.Serialize(new {Catalog=catalog,Result="PASS",CompletedUtc=DateTime.UtcNow}));
    }
}
