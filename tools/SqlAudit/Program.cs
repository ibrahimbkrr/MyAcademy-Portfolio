using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;
using MyAcademy_PortfolioProject.Seed;

// Explicitly opt-in. Never use the configured catalog for any operation.
if (args.Length != 2 || args[0] != "--run")
{
    Console.WriteLine("Usage: dotnet run --project tools/SqlAudit -c Release -- --run <appsettings.json>");
    return 2;
}
var source = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(args[1])).Build();
var name = "PortfolioAudit_" + Guid.NewGuid().ToString("N");
var connection = new SqlConnectionStringBuilder(source.GetConnectionString("DefaultConnection")) { InitialCatalog=name,ConnectTimeout=10 };
var options = new DbContextOptionsBuilder<PortfolioContext>().UseSqlServer(connection.ConnectionString).Options;
await using var db = new PortfolioContext(options);
var created = false;
try
{
    // Generated catalog and equality checks make accidental development DB writes/deletes impossible.
    if(db.Database.GetDbConnection().Database != name || !name.StartsWith("PortfolioAudit_")) throw new InvalidOperationException("Unsafe test catalog");
    created=true;
    await db.Database.MigrateAsync();
    Console.WriteLine("PASS: SQL Server migrations applied to disposable catalog ("+(await db.Database.GetAppliedMigrationsAsync()).Count()+")");
    if(await db.Database.GetPendingMigrationsAsync() is var pending && pending.Any()) throw new InvalidOperationException("Pending migrations");
    var services = new ServiceCollection();
    services.AddLogging(); services.AddDbContext<PortfolioContext>(o=>o.UseSqlServer(connection.ConnectionString));
    services.AddIdentityCore<AppUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<PortfolioContext>();
    using var provider = services.BuildServiceProvider();
    var password = "Aa1!"+Guid.NewGuid().ToString("N");
    var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["AdminSeed:Email"]="audit@example.test", ["AdminSeed:Password"]=password }).Build();
    await IdentitySeeder.SeedAsync(provider,config); await IdentitySeeder.SeedAsync(provider,config);
    using(var scope=provider.CreateScope())
    {
        var users=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user=await users.FindByEmailAsync("audit@example.test");
        if(user==null || !await users.CheckPasswordAsync(user,password) || !await users.IsInRoleAsync(user,"Admin")) throw new InvalidOperationException("Identity roundtrip failed");
    }
    Console.WriteLine("PASS: SQL Server Identity seed, password verification, Admin role and repeat seed");
    var social = new SocialMedia { Platform="Audit",Url="https://example.test",IsActive=true };
    db.SocialMedias.Add(social); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    social=await db.SocialMedias.SingleAsync(); social.Platform="Updated"; await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    social=await db.SocialMedias.SingleAsync(); if(social.Platform!="Updated") throw new InvalidOperationException("CRUD update failed");
    db.SocialMedias.Remove(social); await db.SaveChangesAsync(); if(await db.SocialMedias.AnyAsync()) throw new InvalidOperationException("CRUD delete failed");
    Console.WriteLine("PASS: SQL Server persisted CRUD roundtrip");
    db.Projects.Add(new Project { ProjectTitle="Invalid FK",Description="Test",CategoryId=int.MaxValue });
    try { await db.SaveChangesAsync(); throw new InvalidOperationException("Foreign key accepted invalid category"); }
    catch(DbUpdateException) { db.ChangeTracker.Clear(); Console.WriteLine("PASS: SQL Server foreign key rejects missing category"); }
    return 0;
}
catch(Exception ex)
{
    // Do not print connection strings, account details or provider exception messages.
    Console.WriteLine("FAIL: SQL audit ("+ex.GetType().Name+")");
    if(ex is SqlException sql) Console.WriteLine($"SQL error number: {sql.Number}; state: {sql.State}; class: {sql.Class}");
    return 1;
}
finally
{
    if(created && db.Database.GetDbConnection().Database == name && name.StartsWith("PortfolioAudit_"))
    {
        Console.WriteLine("RETAINED: " + name + " (database deletion is disabled; review separately)");
    }
}
