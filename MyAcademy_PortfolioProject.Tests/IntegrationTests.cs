using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;
using MyAcademy_PortfolioProject.Seed;

namespace MyAcademy_PortfolioProject.Tests;

public partial class IntegrationTests : IDisposable
{
    private readonly PortfolioFactory factory = new();
    private readonly HttpClient client;
    private readonly string password = "Aa1!" + Guid.NewGuid().ToString("N");
    public IntegrationTests() => client = factory.NewClient();
    public void Dispose() { client.Dispose(); factory.Dispose(); }

    private async Task<string> Token(string path = "/Admin/Account/Login")
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Antiforgery field missing: " + path);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private async Task<HttpResponseMessage> Post(string path, Dictionary<string,string> fields, string tokenPage = "/Admin/Account/Login")
    {
        fields["__RequestVerificationToken"] = await Token(tokenPage);
        return await client.PostAsync(path, new FormUrlEncodedContent(fields));
    }

    private async Task CreateUser(string email = "admin@example.test", bool admin = true, bool locked = false)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser { UserName = email, Email = email, FirstName = "Test", LastName = "User" };
        Assert.True((await users.CreateAsync(user, password)).Succeeded);
        if (admin)
        {
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync("Admin")) Assert.True((await roles.CreateAsync(new("Admin"))).Succeeded);
            Assert.True((await users.AddToRoleAsync(user,"Admin")).Succeeded);
        }
        if (locked) Assert.True((await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1))).Succeeded);
    }

    private Task<HttpResponseMessage> Login(bool remember = false, string email = "admin@example.test", string? suppliedPassword = null) =>
        Post("/Admin/Account/Login", new() { ["Email"] = email, ["Password"] = suppliedPassword ?? password, ["RememberMe"] = remember.ToString() });

    private async Task Admin()
    {
        await CreateUser();
        Assert.Equal(HttpStatusCode.Redirect, (await Login()).StatusCode);
    }

    [Theory]
    [InlineData("/Admin/Account/Login")]
    [InlineData("/Admin/Account/AccessDenied")]
    [InlineData("/Default/Index")]
    public async Task Anonymous_public_pages_load(string path) => Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);

    [Fact]
    public async Task Root_renders_portfolio() => Assert.Contains("id=\"home\"", await client.GetStringAsync("/"));

    [Fact]
    public async Task Legacy_experience_route_redirects_to_public_section()
    {
        var response=await client.GetAsync("/Experience/Index");
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        Assert.EndsWith("#experience",response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("Dashboard/Index")]
    [InlineData("HeroSection/EditHero")]
    [InlineData("HeroTitle/HeroTitleList")]
    [InlineData("About/EditAbout")]
    [InlineData("SocialMedia/SocialMediaList")]
    [InlineData("Skill/SkillList")]
    [InlineData("SkillCategory/CategoryList")]
    [InlineData("Experience/ExperienceList")]
    [InlineData("Project/ProjectList")]
    [InlineData("Category/CategoryList")]
    [InlineData("Education/EducationList")]
    [InlineData("EducationContent/EditContent")]
    [InlineData("Contact/EditContact")]
    [InlineData("Message/MessageList")]
    [InlineData("Philosophy/EditPhilosophy")]
    [InlineData("Footer/EditFooter")]
    public async Task Admin_pages_require_authentication_and_load_after_login(string route)
    {
        var response = await client.GetAsync("/Admin/" + route);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Admin/Account/Login?ReturnUrl=", response.Headers.Location!.ToString());
        await Admin();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Admin/" + route)).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Login_cookie_remember_me_and_logout(bool remember)
    {
        await CreateUser();
        var response = await Login(remember);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith(".AspNetCore.Identity.Application="));
        Assert.Contains("secure", cookie); Assert.Contains("httponly", cookie); Assert.Contains("samesite=lax", cookie);
        Assert.Equal(remember, cookie.Contains("expires="));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/Account/Logout", new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
        response = await Post("/Admin/Account/Logout", new(), "/Admin/Footer/EditFooter");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Admin/Dashboard/Index")).StatusCode);
    }

    [Theory]
    [InlineData("admin@example.test", "wrong")]
    [InlineData("missing@example.test", "wrong")]
    [InlineData("invalid", "wrong")]
    [InlineData("admin@example.test", "")]
    public async Task Invalid_login_does_not_authenticate(string email, string input)
    {
        await CreateUser();
        Assert.Equal(HttpStatusCode.OK, (await Login(email:email, suppliedPassword:input)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Admin/Dashboard/Index")).StatusCode);
    }

    [Fact]
    public async Task Locked_user_cannot_login()
    {
        await CreateUser(locked:true);
        Assert.Equal(HttpStatusCode.OK, (await Login()).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Admin/Dashboard/Index")).StatusCode);
    }

    [Fact]
    public async Task Non_admin_cannot_login()
    {
        await CreateUser(admin:false);
        Assert.Equal(HttpStatusCode.OK, (await Login()).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Admin/Dashboard/Index")).StatusCode);
    }

    [Fact]
    public async Task Login_requires_antiforgery() => Assert.Equal(HttpStatusCode.BadRequest,
        (await client.PostAsync("/Admin/Account/Login",new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);

    [Fact]
    public async Task Seeder_must_not_promote_existing_user()
    {
        await CreateUser(admin:false);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["AdminSeed:Email"] = "admin@example.test", ["AdminSeed:Password"] = password }).Build();
        await Assert.ThrowsAsync<InvalidOperationException>(() => IdentitySeeder.SeedAsync(factory.Services, config));
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.False(await users.IsInRoleAsync((await users.FindByEmailAsync("admin@example.test"))!, "Admin"));
    }

    [Fact]
    public async Task Seeder_is_idempotent_and_created_admin_can_login()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["AdminSeed:Email"] = "admin@example.test", ["AdminSeed:Password"] = password }).Build();
        await IdentitySeeder.SeedAsync(factory.Services, config);
        await IdentitySeeder.SeedAsync(factory.Services, config);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        Assert.Equal(1, await db.Users.CountAsync()); Assert.Equal(1, await db.Roles.CountAsync());
        Assert.Equal(HttpStatusCode.Redirect,(await Login()).StatusCode);
    }

    [Theory]
    [InlineData("Footer/EditFooter", "BrandName", "Footers")]
    [InlineData("Philosophy/EditPhilosophy", "Quote", "Philosophies")]
    [InlineData("Contact/EditContact", "Title", "Contacts")]
    [InlineData("HeroSection/EditHero", "Title", "HeroSections")]
    [InlineData("EducationContent/EditContent", "SectionTitle", "EducationContents")]
    public async Task Singleton_updates_persist_once_and_render_publicly(string route, string field, string table)
    {
        await Admin();
        for (int i=0; i<2; i++)
        {
            var fields = new Dictionary<string,string> { [field] = "AuditMarker" + i };
            if (table == "Contacts") { fields["SectionLabel"] = "Contact"; fields["Email"] = "contact@example.test"; }
            if (table == "HeroSections") { fields["Greeting"]="Hello"; fields["Description"]="Test"; }
            Assert.Equal(HttpStatusCode.Redirect, (await Post("/Admin/"+route, fields, "/Admin/"+route)).StatusCode);
        }
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        var count = table switch {
            "Footers" => await db.Footers.CountAsync(), "Contacts" => await db.Contacts.CountAsync(),
            "HeroSections" => await db.HeroSections.CountAsync(), "EducationContents" => await db.EducationContents.CountAsync(),
            _ => await db.Philosophies.CountAsync() };
        Assert.Equal(1,count);
        Assert.Contains("AuditMarker1",await client.GetStringAsync("/Default/Index"));
        Assert.Equal(HttpStatusCode.OK, (await Post("/Admin/"+route,new() { [field]="" }, "/Admin/"+route)).StatusCode);
    }
}
