using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PORTFOLIO_SQL_TEST_CONFIG")))
            Skip="Requires PORTFOLIO_SQL_TEST_CONFIG; creates and retains an isolated SQL database.";
    }
}

public sealed class SqlAuditFactory : WebApplicationFactory<Program>
{
    public string Catalog { get; }="PortfolioPhase2_"+Guid.NewGuid().ToString("N");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var source=new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(Environment.GetEnvironmentVariable("PORTFOLIO_SQL_TEST_CONFIG")!)).Build();
        var connection=new SqlConnectionStringBuilder(source.GetConnectionString("DefaultConnection")) { InitialCatalog=Catalog, ConnectTimeout=10 };
        if(!Catalog.StartsWith("PortfolioPhase2_") || Catalog.Length!=48) throw new InvalidOperationException("Invalid isolated catalog");
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging=>logging.ClearProviders());
        builder.ConfigureServices(services=> {
            services.RemoveAll<DbContextOptions<PortfolioContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<PortfolioContext>>();
            services.AddDbContext<PortfolioContext>(options=>options.UseSqlServer(connection.ConnectionString));
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }
    public HttpClient Client()=>CreateClient(new() { BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false });
    public async Task RecordCatalogAsync()
    {
        var directory=Path.Combine(AppContext.BaseDirectory,"sql-results"); Directory.CreateDirectory(directory);
        await File.AppendAllTextAsync(Path.Combine(directory,"retained-catalogs.txt"),Catalog+Environment.NewLine);
    }
}

public class SqlServerTests
{
    [SqlServerFact]
    public async Task Sql_distinct_category_name_allows_migration_without_changing_rows_or_relationships()
    {
        using var factory=new SqlAuditFactory(); using var client=factory.Client(); await factory.RecordCatalogAsync();
        using var scope=factory.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        await db.GetService<IMigrator>().MigrateAsync("20261116184236_AddIdentityTables");
        var first=new Category {CategoryName="Test Otomasyonu",FilterKey=null};
        var second=new Category {CategoryName="Test Otomasyonu",FilterKey="automation"};
        var third=new Category {CategoryName="Other",FilterKey=null};
        db.Categories.AddRange(first,second,third); await db.SaveChangesAsync();
        db.Projects.Add(new Project {ProjectTitle="Synthetic relationship",Description="Rehearsal only",CategoryId=second.CategoryId});
        await db.SaveChangesAsync();
        var before=await db.Projects.AsNoTracking().Select(p=>new {p.ProjectId,p.CategoryId}).ToListAsync();
        first.CategoryName="Test Otomasyonu (Arşiv)"; // Hypothesis only; not a choice for real data.
        await db.SaveChangesAsync();
        await db.Database.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(3,await db.Categories.CountAsync());
        Assert.Equal(before,await db.Projects.AsNoTracking().Select(p=>new {p.ProjectId,p.CategoryId}).ToListAsync());
        Assert.Equal(2,await db.Categories.CountAsync(c=>c.FilterKey==null));
        Assert.Equal(0,await db.Projects.CountAsync(p=>!db.Categories.Any(c=>c.CategoryId==p.CategoryId)));
    }

    private static async Task<string> Token(HttpClient client,string page)
    {
        var html=await client.GetStringAsync(page);
        var match=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success); return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    [SqlServerFact]
    public async Task Sql_clean_migrations_uniqueness_concurrent_singletons_and_preserved_ids()
    {
        using var factory=new SqlAuditFactory(); using var client=factory.Client();
        await factory.RecordCatalogAsync();
        using var scope=factory.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        await db.Database.MigrateAsync(); Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        var password="Aa1!"+Guid.NewGuid().ToString("N");
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["AdminSeed:Email"]="sql@example.test",["AdminSeed:Password"]=password }).Build();
        await MyAcademy_PortfolioProject.Seed.IdentitySeeder.SeedAsync(factory.Services,config);
        await MyAcademy_PortfolioProject.Seed.IdentitySeeder.SeedAsync(factory.Services,config);
        var login=await client.PostAsync("/Admin/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string> { ["Email"]="sql@example.test",["Password"]=password,["__RequestVerificationToken"]=await Token(client,"/Admin/Account/Login") }));
        Assert.Equal(HttpStatusCode.Redirect,login.StatusCode);
        foreach(var scenario in new[] {
            ("Footer/EditFooter","Footers","FooterId",new Dictionary<string,string>{{"BrandName","SQL marker"}}),
            ("Contact/EditContact","Contacts","ContactId",new Dictionary<string,string>{{"Title","SQL marker"},{"SectionLabel","Test"},{"Email","sql@example.test"}}),
            ("Philosophy/EditPhilosophy","Philosophies","PhilosophyId",new Dictionary<string,string>{{"Quote","SQL marker"}}),
            ("EducationContent/EditContent","EducationContents","EducationContentId",new Dictionary<string,string>{{"SectionTitle","SQL marker"}}),
            ("HeroSection/EditHero","HeroSections","HeroSectionId",new Dictionary<string,string>{{"Greeting","Hello"},{"Title","SQL marker"},{"Description","Test"}}),
            ("About/EditAbout","Abouts","AboutId",new Dictionary<string,string>{{"Name","SQL marker"},{"Title","Test"}}) })
        {
            var path="/Admin/"+scenario.Item1;
            scenario.Item4["__RequestVerificationToken"]=await Token(client,path);
            var responses=await Task.WhenAll(Enumerable.Range(0,6).Select(_=>client.PostAsync(path,new FormUrlEncodedContent(scenario.Item4))));
            Assert.All(responses,response=>Assert.Equal(HttpStatusCode.Redirect,response.StatusCode));
            using var command=db.Database.GetDbConnection().CreateCommand(); await db.Database.OpenConnectionAsync();
            command.CommandText=$"SELECT COUNT(*) FROM {scenario.Item2}"; Assert.Equal(1,Convert.ToInt32(await command.ExecuteScalarAsync()));
            command.CommandText=$"SELECT MIN({scenario.Item3}) FROM {scenario.Item2}"; var id=Convert.ToInt32(await command.ExecuteScalarAsync());
            Assert.Equal(HttpStatusCode.Redirect,(await client.PostAsync(path,new FormUrlEncodedContent(scenario.Item4))).StatusCode);
            Assert.Equal(id,Convert.ToInt32(await command.ExecuteScalarAsync())); await db.Database.CloseConnectionAsync();
        }
        var categoryPath="/Admin/Category/CreateCategory";
        var fields=new Dictionary<string,string>{{"CategoryName","Parallel category"},{"FilterKey","parallel-category"},{"__RequestVerificationToken",await Token(client,categoryPath)}};
        var creates=await Task.WhenAll(Enumerable.Range(0,6).Select(_=>client.PostAsync(categoryPath,new FormUrlEncodedContent(fields))));
        Assert.Equal(1,creates.Count(r=>r.StatusCode==HttpStatusCode.Redirect));
        Assert.All(creates,r=>Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect));
        Assert.Equal(1,await db.Categories.CountAsync());
        db.Categories.Add(new Category { CategoryName="Parallel category",FilterKey="other" });
        await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync()); db.ChangeTracker.Clear();
        // SQL Server ignores trailing spaces according to its collation comparison semantics.
        db.Categories.Add(new Category { CategoryName="Parallel category   ",FilterKey="other" });
        await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync()); db.ChangeTracker.Clear();
        db.SkillCategories.Add(new SkillCategory { CategoryName="SQL Skill" }); await db.SaveChangesAsync();
        db.SkillCategories.Add(new SkillCategory { CategoryName="SQL Skill" });
        await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task Sql_migration_preflight_rejects_duplicates_without_changing_existing_records()
    {
        using var factory=new SqlAuditFactory(); using var client=factory.Client(); await factory.RecordCatalogAsync();
        using var scope=factory.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        await db.GetService<IMigrator>().MigrateAsync("20261116184236_AddIdentityTables");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Categories (CategoryName,FilterKey) VALUES (N'Duplicate',N'duplicate'),(N'Duplicate',N'duplicate');");
        var error=await Assert.ThrowsAsync<SqlException>(()=>db.Database.MigrateAsync()); Assert.Equal(51010,error.Number);
        Assert.Equal(2,await db.Categories.CountAsync());
        Assert.DoesNotContain((await db.Database.GetAppliedMigrationsAsync()),x=>x.EndsWith("EnforceCategoryUniqueness"));
    }

    [SqlServerFact]
    public async Task Sql_legacy_populated_skill_upgrade_is_blocked_and_rolls_back_data_loss()
    {
        using var factory=new SqlAuditFactory(); using var client=factory.Client(); await factory.RecordCatalogAsync();
        using var scope=factory.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        await db.GetService<IMigrator>().MigrateAsync("20261020115115_mig2");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO Skills (Title,Description,IconUrl) VALUES (N'Legacy skill',N'Preserve description',N'legacy.png'); INSERT INTO HeroSections (Greating,Title,Description,MainImageUrl) VALUES (N'Preserve greeting',N'Test',N'Test',N'test.png');");
        await db.GetService<IMigrator>().MigrateAsync("20261112195941_mig3");
        var error=await Assert.ThrowsAsync<SqlException>(()=>db.Database.MigrateAsync()); Assert.Equal(547,error.Number);
        await db.Database.OpenConnectionAsync(); using var command=db.Database.GetDbConnection().CreateCommand();
        command.CommandText="SELECT Description FROM Skills"; Assert.Equal("Preserve description",await command.ExecuteScalarAsync());
        command.CommandText="SELECT Greeting FROM HeroSections"; Assert.Equal("Preserve greeting",await command.ExecuteScalarAsync());
        await db.Database.CloseConnectionAsync();
    }
}
