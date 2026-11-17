using Microsoft.Playwright;

namespace MyAcademy_PortfolioProject.Tests;

public partial class IntegrationTests
{
    private static async Task Phase2BrowserFlow(IPage page,string origin,int width)
    {
        await page.GotoAsync(origin+"/Admin/SocialMedia/CreateSocialMedia");
        await Assertions.Expect(page.Locator(".breadcrumb-custom")).ToContainTextAsync("Sosyal");
        await Assertions.Expect(page.Locator(".sidebar-link.active")).ToContainTextAsync("Sosyal");
        await page.Locator("#Platform").FillAsync("BrowserSocial");
        await page.Locator("#Url").FillAsync("https://example.test/profile");
        await Assertions.Expect(page.Locator("#socialPreviewPlatform")).ToHaveTextAsync("BrowserSocial");
        await page.Locator("#createSocialButton").ClickAsync();
        await page.WaitForURLAsync("**/SocialMedia/SocialMediaList");
        var row=page.Locator("tr").Filter(new() { HasText="BrowserSocial" });
        await Assertions.Expect(row).ToBeVisibleAsync();
        await page.Locator("#socialSearch").FillAsync("absent"); await Assertions.Expect(row).ToBeHiddenAsync();
        await page.Locator("#socialSearch").FillAsync("");
        await row.Locator("a[href*='UpdateSocialMedia']").ClickAsync();
        await page.Locator("#IsActive").UncheckAsync();
        await page.Locator("form[id] button[type=submit]").Last.ClickAsync();
        await page.WaitForURLAsync("**/SocialMedia/SocialMediaList");
        await page.Locator("#socialStatusFilter").SelectOptionAsync("active"); await Assertions.Expect(row).ToBeHiddenAsync();
        await page.Locator("#socialStatusFilter").SelectOptionAsync("passive"); await Assertions.Expect(row).ToBeVisibleAsync();
        var confirmCount=0;
        async void DialogHandler(object? sender,IDialog dialog) { confirmCount++; if(confirmCount==1) await dialog.DismissAsync(); else await dialog.AcceptAsync(); }
        page.Dialog+=DialogHandler;
        await row.Locator("form button[type=submit]").ClickAsync(); await Assertions.Expect(row).ToBeVisibleAsync();
        await row.Locator("form button[type=submit]").ClickAsync(); await Assertions.Expect(row).ToHaveCountAsync(0);
        page.Dialog-=DialogHandler; Assert.Equal(2,confirmCount);

        await page.GotoAsync(origin+"/Admin/About/EditAbout");
        await page.Locator("#ImageFile").SetInputFilesAsync(new FilePayload { Name="preview.png",MimeType="image/png",Buffer=ValidPng() });
        await Assertions.Expect(page.Locator("#newAboutImagePreview")).ToBeVisibleAsync();
        await page.Locator("#CvFile").SetInputFilesAsync(new FilePayload { Name="preview.pdf",MimeType="application/pdf",Buffer="%PDF-1.4\n%%EOF"u8.ToArray() });
        await Assertions.Expect(page.Locator("#selectedCvName")).ToContainTextAsync("preview.pdf");
        // UI preview is tested without writing into the real wwwroot. Persistence/cleanup has isolated HTTP tests.
        await page.Locator("#ImageFile").SetInputFilesAsync(Array.Empty<string>());
        await page.Locator("#CvFile").SetInputFilesAsync(Array.Empty<string>());

        await page.GotoAsync(origin+"/Admin/Experience/CreateExperience");
        await page.Locator("#Title").FillAsync("BrowserExperience");
        await page.Locator("#CompanyName").FillAsync("Test Company");
        await page.Locator("#CategoryName").SelectOptionAsync("Freelance");
        await page.Locator("#Description").FillAsync("Browser description");
        await page.Locator("#StartDate").FillAsync("2025-01-01");
        await page.Locator("#EndDate").FillAsync("2024-01-01");
        await page.Locator("#createExperienceButton").ClickAsync();
        await Assertions.Expect(page.Locator("[data-valmsg-for=EndDate]")).Not.ToBeEmptyAsync();
        await page.Locator("#IsCurrent").CheckAsync();
        await Assertions.Expect(page.Locator("#EndDate")).ToBeDisabledAsync();
        await page.Locator("#createExperienceButton").ClickAsync();
        await page.WaitForURLAsync("**/Experience/ExperienceList");
        await Assertions.Expect(page.Locator(".experience-data-row")).ToContainTextAsync("BrowserExperience");
        await page.Locator("#tableSearchInput").FillAsync("absent");
        await Assertions.Expect(page.Locator(".experience-data-row")).ToBeHiddenAsync();
        await page.Locator("#btnResetFilters").ClickAsync();
        await Assertions.Expect(page.Locator(".experience-data-row")).ToBeVisibleAsync();
        await page.Locator(".admin-navbar .btn-quick-add").ClickAsync();
        await page.WaitForURLAsync("**/Skill/CreateSkill");
        await Assertions.Expect(page.Locator(".breadcrumb-custom")).ToContainTextAsync("Yeni Yetenek");
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth + 1"));
        await page.Locator(".admin-navbar .icon-action-btn").ClickAsync();
        await page.WaitForURLAsync("**/Message/MessageList");
        await page.Locator(".breadcrumb-custom a").First.ClickAsync();
        await page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(@"/Admin(?:/Dashboard(?:/Index)?)?/?$"));
        if(width<992)
        {
            await page.Locator("#sidebarToggle").ClickAsync();
            await Assertions.Expect(page.Locator("#adminSidebar")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("show-sidebar"));
            // The drawer covers the opener; its supported dismissal is clicking outside.
            await page.Mouse.ClickAsync(width-5,10);
            await Assertions.Expect(page.Locator("#adminSidebar")).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("show-sidebar"));
        }
        await page.GotoAsync(origin+"/Admin/Experience/ExperienceList");
        await page.EvaluateAsync("window.scrollTo(0,0)");
        var output=Path.Combine(AppContext.BaseDirectory,"browser-results"); Directory.CreateDirectory(output);
        await page.ScreenshotAsync(new() { Path=Path.Combine(output,$"phase2-admin-{width}.png"),FullPage=true });
        Assert.True(await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth + 1"), await page.EvaluateAsync<string>("JSON.stringify([...document.querySelectorAll('body *')].filter(e=>e.getBoundingClientRect().right>innerWidth+1).map(e=>({tag:e.tagName,cls:e.className,width:e.getBoundingClientRect().width})).slice(0,15))"));
    }
}
