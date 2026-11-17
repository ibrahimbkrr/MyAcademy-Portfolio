using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Tests;

public partial class IntegrationTests
{
    [Fact]
    public async Task Authenticated_non_admin_is_forbidden()
    {
        await CreateUser(admin:false);
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<AppUser>>();
        var principal = await signIn.CreateUserPrincipalAsync((await users.FindByEmailAsync("admin@example.test"))!);
        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);
        var ticket = new AuthenticationTicket(principal,new AuthenticationProperties(),IdentityConstants.ApplicationScheme);
        using var nonAdmin = factory.NewClient();
        nonAdmin.DefaultRequestHeaders.Add("Cookie",options.Cookie.Name+"="+options.TicketDataFormat.Protect(ticket));
        var response = await nonAdmin.GetAsync("/Admin/Dashboard/Index");
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        Assert.Contains("/Admin/Account/AccessDenied",response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("SocialMedia/CreateSocialMedia", "Url", "javascript:alert(1)")]
    [InlineData("Contact/EditContact", "Email", "invalid")]
    [InlineData("Skill/CreateSkill", "Level", "101")]
    [InlineData("Education/CreateEducation", "EndDate", "2020-01-01")]
    [InlineData("Experience/CreateExperience", "EndDate", "2020-01-01")]
    [InlineData("Footer/EditFooter", "BrandName", "long")]
    public async Task Invalid_inputs_are_not_persisted(string route,string field,string value)
    {
        await Admin();
        var fields = new Dictionary<string,string> { ["Title"]="Test", ["SectionLabel"]="Test", ["Email"]="test@example.test", ["Platform"]="Test", ["Url"]="https://example.test", ["SkillName"]="Test", ["SkillCategoryId"]="1", ["Level"]="50", ["Institution"]="Test", ["EducationType"]="Course", ["StartDate"]="2021-01-01", ["CompanyName"]="Test", ["Description"]="Test", ["CategoryName"]="Work", ["BrandName"]="Test" };
        fields[field] = value == "long" ? new string('a',101) : value;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        db.SkillCategories.Add(new SkillCategory { CategoryName="Test" }); await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.OK,(await Post("/Admin/"+route,fields,"/Admin/"+route)).StatusCode);
        Assert.Equal(0,await db.SocialMedias.CountAsync()+await db.Contacts.CountAsync()+await db.Skills.CountAsync()+await db.Educations.CountAsync()+await db.Experiences.CountAsync()+await db.Footers.CountAsync());
    }

    [Theory]
    [InlineData("HeroSection/EditHero","ImageFile","fake.png")]
    [InlineData("About/EditAbout","ImageFile","fake.png")]
    [InlineData("About/EditAbout","CvFile","fake.pdf")]
    [InlineData("Project/CreateProject","ImageFile","fake.png")]
    [InlineData("About/EditAbout","ImageFile","evil.svg")]
    public async Task Upload_rejects_invalid_signature_or_extension(string route,string field,string name)
    {
        await Admin();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        var category = new Category { CategoryName="Test",FilterKey="test" }; db.Categories.Add(category); await db.SaveChangesAsync();
        using var form = new MultipartFormDataContent();
        foreach(var item in new Dictionary<string,string> { ["Name"]="Test", ["Title"]="Test", ["Greeting"]="Test", ["Description"]="Test", ["ProjectTitle"]="Test", ["CategoryId"]=category.CategoryId.ToString(), ["__RequestVerificationToken"]=await Token("/Admin/"+route) }) form.Add(new StringContent(item.Value),item.Key);
        form.Add(new ByteArrayContent("not a valid file"u8.ToArray()),field,name);
        Assert.Equal(HttpStatusCode.OK,(await client.PostAsync("/Admin/"+route,form)).StatusCode);
        Assert.Equal(0,await db.HeroSections.CountAsync()+await db.Abouts.CountAsync()+await db.Projects.CountAsync());
    }

    [Fact]
    public async Task About_uploads_use_generated_names_and_singleton_updates_preserve_files()
    {
        await Admin();
        var environment = factory.Services.GetRequiredService<IWebHostEnvironment>();
        // All uploaded bytes go to a unique test-owned directory, never the application's assets.
        var root = Path.Combine(AppContext.BaseDirectory,"upload-results",Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); environment.WebRootPath=root;
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(await Token("/Admin/About/EditAbout")),"__RequestVerificationToken");
        form.Add(new StringContent("UploadAudit"),"Name"); form.Add(new StringContent("UploadAudit"),"Title");
        var png = ValidPng();
        form.Add(new ByteArrayContent(png),"ImageFile","../../photo.png");
        form.Add(new ByteArrayContent("%PDF-1.4\n%%EOF"u8.ToArray()),"CvFile","../../resume.pdf");
        Assert.Equal(HttpStatusCode.Redirect,(await client.PostAsync("/Admin/About/EditAbout",form)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        var about = await db.Abouts.AsNoTracking().SingleAsync();
        Assert.Matches("^/images/about/[a-f0-9]{32}\\.png$",about.ImageUrl!);
        Assert.Matches("^/files/cv/[a-f0-9]{32}\\.pdf$",about.CvUrl!);
        var imagePath=Path.Combine(root,about.ImageUrl!.TrimStart('/')); var cvPath=Path.Combine(root,about.CvUrl!.TrimStart('/'));
        Assert.True(File.Exists(imagePath)); Assert.True(File.Exists(cvPath));
        Assert.Equal(HttpStatusCode.Redirect,(await Post("/Admin/About/EditAbout",new() { ["Name"]="Updated", ["Title"]="Updated", ["ExistingImageUrl"]="/untrusted.png" },"/Admin/About/EditAbout")).StatusCode);
        var updated=await db.Abouts.AsNoTracking().SingleAsync(); Assert.Equal(about.ImageUrl,updated.ImageUrl); Assert.Equal(about.CvUrl,updated.CvUrl);
        File.Delete(imagePath); File.Delete(cvPath);
    }
}
