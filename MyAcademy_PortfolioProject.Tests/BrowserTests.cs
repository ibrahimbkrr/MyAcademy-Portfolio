using Microsoft.Playwright;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Tests;

public partial class IntegrationTests
{
    [Theory]
    [InlineData(1440,900)]
    [InlineData(768,1024)]
    [InlineData(390,844)]
    public async Task Browser_login_admin_navigation_and_public_page(int width, int height)
    {
        await CreateUser();
        using(var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
            db.HeroTitles.Add(new HeroTitle { Title="Browser audit", IsActive=true });
            await db.SaveChangesAsync();
        }
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Channel="msedge", Headless=true });
        var context = await browser.NewContextAsync(new() { ViewportSize=new() { Width=width,Height=height } });
        var errors = new List<string>();
        var failures = new List<string>();
        var page = await context.NewPageAsync();
        var output = Path.Combine(AppContext.BaseDirectory,"browser-results"); Directory.CreateDirectory(output);
        page.PageError += (_,error) => errors.Add(error);
        page.RequestFailed += (_,request) => failures.Add(request.Url + ": " + request.Failure);
        page.Response += (_,response) => { if(response.Status>=400) failures.Add(response.Url+": "+response.Status); };
        // Local transport bridge to the real MVC TestServer; its HTTPS client owns the test cookie jar.
        // Browser tests cover DOM/JS/forms; secure cookie transport is tested separately over HTTPS.
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var bridge = builder.Build();
        bridge.Run(async http =>
        {
            using var request = new HttpRequestMessage(new HttpMethod(http.Request.Method),"https://localhost"+http.Request.Path+http.Request.QueryString);
            if(http.Request.ContentLength>0) { using var body = new MemoryStream(); await http.Request.Body.CopyToAsync(body); request.Content = new ByteArrayContent(body.ToArray()); }
            foreach(var header in http.Request.Headers)
            {
                if(header.Key.Equals("host",StringComparison.OrdinalIgnoreCase) || header.Key.Equals("content-length",StringComparison.OrdinalIgnoreCase) || header.Key.Equals("cookie",StringComparison.OrdinalIgnoreCase)) continue;
                if(!request.Headers.TryAddWithoutValidation(header.Key,header.Value.ToArray())) request.Content?.Headers.TryAddWithoutValidation(header.Key,header.Value.ToArray());
            }
            using var response = await client.SendAsync(request);
            http.Response.StatusCode=(int)response.StatusCode;
            foreach(var header in response.Headers.Concat(response.Content.Headers))
                if(header.Key is not "Transfer-Encoding" and not "Set-Cookie") http.Response.Headers[header.Key]=header.Value.ToArray();
            if(response.Headers.Location is { IsAbsoluteUri:true } location && location.Host=="localhost")
                http.Response.Headers.Location=$"{http.Request.Scheme}://{http.Request.Host}{location.PathAndQuery}{location.Fragment}";
            await response.Content.CopyToAsync(http.Response.Body);
        });
        await bridge.StartAsync();
        var origin=bridge.Urls.Single();
        await page.GotoAsync(origin+"/Admin/Account/Login");
        await Assertions.Expect(page.Locator("#Email")).ToBeVisibleAsync();
        await page.ScreenshotAsync(new() { Path=Path.Combine(output,$"login-{width}.png"),FullPage=true });
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth + 1"));
        await page.Locator("#Email").FillAsync("admin@example.test");
        await page.Locator("#Password").FillAsync(password);
        await page.Locator("button[type=submit]").ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@"/Admin(?:/Dashboard(?:/Index)?)?/?$"), new() { WaitUntil=WaitUntilState.DOMContentLoaded });
        if(width<992)
        {
            await page.Locator("#sidebarToggle").ClickAsync();
            await Assertions.Expect(page.Locator("#adminSidebar")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("show-sidebar"));
        }
        await Phase2BrowserFlow(page,origin,width);
        await page.GotoAsync(origin+"/Admin/Footer/EditFooter");
        await page.Locator("#BrandName").FillAsync("BrowserAudit");
        await page.Locator("button[type=submit]").Last.ClickAsync();
        await page.WaitForLoadStateAsync();
        await page.GotoAsync(origin+"/Admin/Experience/ExperienceList");
        await page.Locator("#tableSearchInput").FillAsync("no-match");
        await page.Locator("#sortSelect").SelectOptionAsync("title-asc");
        await page.Locator("#btnResetFilters").ClickAsync();
        await page.GotoAsync(origin+"/");
        await Assertions.Expect(page.Locator("footer")).ToContainTextAsync("BrowserAudit");
        await Assertions.Expect(page.Locator("#typewriterText")).Not.ToBeEmptyAsync();
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth + 1"));
        foreach(var section in await page.Locator("section[id]").AllAsync())
            await section.ScrollIntoViewIfNeededAsync();
        await page.Locator("#home").ScrollIntoViewIfNeededAsync();
        await page.ScreenshotAsync(new() { Path=Path.Combine(output,$"public-{width}.png"),FullPage=true });
        await File.WriteAllLinesAsync(Path.Combine(output,$"network-{width}.txt"),failures.Distinct());
        await File.WriteAllLinesAsync(Path.Combine(output,$"console-{width}.txt"),errors);
        await page.GotoAsync(origin+"/Admin/Footer/EditFooter");
        if(width<992) await page.Locator("#sidebarToggle").ClickAsync();
        await page.Locator("#logoutForm button").ClickAsync();
        await page.WaitForURLAsync("**/Admin/Account/Login");
        await page.GotoAsync(origin+"/Admin/Dashboard/Index");
        await Assertions.Expect(page.Locator("#Email")).ToBeVisibleAsync();
        Assert.True(errors.Count==0,string.Join("\n",errors));
        Assert.DoesNotContain(failures,x => x.StartsWith(origin));
    }
}
