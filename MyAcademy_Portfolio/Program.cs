
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Conventions;
using MyAcademy_PortfolioProject.Entities;
using MyAcademy_PortfolioProject.Seed;
using MyAcademy_PortfolioProject.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostingSecurity(builder.Configuration);
builder.Services.AddScoped<BrandingProvider>();
builder.Services.AddScoped<UploadStorage>();
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck>("database",tags: ["ready"]);

// ======================================================
// 1. MVC + ADMIN AUTHORIZATION
// ======================================================

builder.Services.AddControllersWithViews(options =>
{
    // Admin Area için merkezi yetkilendirme
    options.Conventions.Add(
        new AdminAuthorizationConvention());
});

// ======================================================
// 2. DATABASE CONNECTION
// ======================================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection bağlantı bilgisi bulunamadı.");

builder.Services.AddDbContext<PortfolioContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// ======================================================
// 3. ASP.NET CORE IDENTITY
// ======================================================

builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        // PASSWORD SETTINGS
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 12;

        // USER SETTINGS
        options.User.RequireUniqueEmail = true;

        // LOCKOUT SETTINGS
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);

        options.Lockout.MaxFailedAccessAttempts = 5;

        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<PortfolioContext>()
    .AddDefaultTokenProviders();

// ======================================================
// 4. COOKIE SETTINGS
// ======================================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Account/Login";

    options.AccessDeniedPath = "/Admin/Account/AccessDenied";

    // Oturum süresi
    options.ExpireTimeSpan = TimeSpan.FromHours(2);

    options.SlidingExpiration = true;

    // Cookie güvenliği
    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite =
        SameSiteMode.Lax;
});

// ======================================================
// 5. BUILD APPLICATION
// ======================================================

var app = builder.Build();

// ======================================================
// 6. IDENTITY SEEDER
// ======================================================

// Yalnızca geliştirme ortamında ilk Admin kurulumu
if (app.Environment.IsDevelopment())
{
    await IdentitySeeder.SeedAsync(
        app.Services,
        app.Configuration);
}

// ======================================================
// 7. ERROR HANDLING
// ======================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ======================================================
// 8. MIDDLEWARE
// ======================================================

if ((builder.Configuration.GetSection("ReverseProxy:TrustedProxies").Get<string[]>() ?? []).Length > 0)
    app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    if (context.Request.Path.StartsWithSegments("/files/cv"))
    {
        context.Response.Headers.ContentDisposition = "attachment";
        context.Response.Headers.ContentSecurityPolicy = "sandbox; default-src 'none'";
    }
    await next();
});

// Statik dosyalar
app.UseStaticFiles();
if (builder.Configuration["Uploads:RootDirectory"] is { Length:>0 } uploadRoot)
{
    // Expose only managed upload directories; never serve the entire configured disk root.
    foreach (var relative in new[] { "images/about", "images/hero", "images/projects", "files/cv" })
    {
        var directory = Path.Combine(Path.GetFullPath(uploadRoot), relative);
        Directory.CreateDirectory(directory);
        app.UseStaticFiles(new StaticFileOptions {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(directory),
            RequestPath = "/" + relative
        });
    }
}

// Routing
app.UseRouting();
app.UseRateLimiter();

// Kimlik doğrulama
app.UseAuthentication();

// Rol ve yetki kontrolü
app.UseAuthorization();

// Statik asset endpointleri
app.MapStaticAssets();
app.MapHealthChecks("/health/live",new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate=_=>false });
app.MapHealthChecks("/health/ready",new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate=check=>check.Tags.Contains("ready") });

// ======================================================
// 9. ROUTES
// ======================================================

// Admin Area
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Default}/{action=Index}/{id?}");

// ======================================================
// 10. RUN APPLICATION
// ======================================================

app.Run();

public partial class Program { }
