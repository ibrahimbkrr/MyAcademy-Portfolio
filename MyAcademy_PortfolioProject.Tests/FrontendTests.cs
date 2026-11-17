using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Tests;

public partial class IntegrationTests
{
    [Fact]
    public async Task Public_components_render_database_content_and_filter_inactive_records()
    {
        using var scope=factory.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        db.HeroSections.Add(new HeroSection { Greeting="DbGreeting",Title="DbHero",MainImageUrl="/images/db-hero.png" });
        db.HeroTitles.AddRange(new HeroTitle { Title="DbAnimatedFirst",Order=1,IsActive=true },new HeroTitle { Title="DbAnimatedSecond",Order=2,IsActive=true },new HeroTitle { Title="HiddenAnimated",IsActive=false });
        db.Abouts.Add(new About { Name="DbAbout",Title="DbAboutTitle",CvUrl="/files/db-cv.pdf" });
        var skillCategory=new SkillCategory { CategoryName="DbSkillCategory" };
        db.Skills.Add(new Skill { SkillName="DbSkill",Level=75,SkillCategory=skillCategory });
        db.Experiences.AddRange(new Experience { Title="DbExperience",CompanyName="DbCompany",Description="DbDescription",CategoryName="Work",IsActive=true },new Experience { Title="HiddenExperience",IsActive=false });
        var category=new Category { CategoryName="DbProjectCategory",FilterKey="db-project" };
        db.Projects.AddRange(new Project { ProjectTitle="DbProject",Description="DbProjectDescription",Category=category,IsActive=true },new Project { ProjectTitle="HiddenProject",Description="Hidden",Category=category,IsActive=false });
        db.Educations.AddRange(new Education { Title="DbEducation",Institution="DbSchool",IsActive=true },new Education { Title="HiddenEducation",IsActive=false });
        db.EducationContents.Add(new EducationContent { SectionTitle="DbEducationHeading" });
        db.Philosophies.Add(new Philosophy { Quote="DbPhilosophy" });
        db.Contacts.Add(new Contact { Title="DbContact",SectionLabel="DbContactLabel",Email="db@example.test" });
        db.SocialMedias.AddRange(new SocialMedia { Platform="DbSocial",Url="https://example.test/db-social",IsActive=true },new SocialMedia { Platform="HiddenSocial",Url="https://example.test/hidden-social",IsActive=false });
        db.Footers.Add(new Footer { BrandName="DbFooter",Description="DbFooterDescription",CopyrightText="DbCopyright" });
        await db.SaveChangesAsync();
        var html=await client.GetStringAsync("/");
        foreach(var marker in new[] { "DbGreeting","DbHero","/images/db-hero.png","DbAnimatedFirst","DbAnimatedSecond","DbAbout","/files/db-cv.pdf","DbSkillCategory","DbSkill","DbExperience","DbProjectCategory","DbProject","DbEducation","DbEducationHeading","DbPhilosophy","DbContact","db@example.test","https://example.test/db-social","DbFooter","DbCopyright" }) Assert.Contains(marker,html);
        foreach(var marker in new[] { "HiddenAnimated","HiddenExperience","HiddenProject","HiddenEducation","hidden-social" }) Assert.DoesNotContain(marker,html);
        Assert.True(html.IndexOf("DbAnimatedFirst",StringComparison.Ordinal)<html.IndexOf("DbAnimatedSecond",StringComparison.Ordinal));
    }

    [Fact]
    public async Task Categories_reject_duplicates_and_deletion_with_children()
    {
        await Admin();
        foreach(var controller in new[] { "Category","SkillCategory" })
        {
            var path=$"/Admin/{controller}/CreateCategory";
            var fields=new Dictionary<string,string> { ["CategoryName"]="Unique",["FilterKey"]="unique" };
            Assert.Equal(System.Net.HttpStatusCode.Redirect,(await Post(path,fields,path)).StatusCode);
            Assert.Equal(System.Net.HttpStatusCode.OK,(await Post(path,fields,path)).StatusCode);
        }
        using var scope=factory.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        var category=await db.Categories.SingleAsync(); var skillCategory=await db.SkillCategories.SingleAsync();
        db.Projects.Add(new Project { ProjectTitle="Child",Description="Test",CategoryId=category.CategoryId });
        db.Skills.Add(new Skill { SkillName="Child",SkillCategoryId=skillCategory.SkillCategoryId }); await db.SaveChangesAsync();
        await Post("/Admin/Category/DeleteCategory",new() { ["id"]=category.CategoryId.ToString() },"/Admin/Footer/EditFooter");
        await Post("/Admin/SkillCategory/DeleteCategory",new() { ["id"]=skillCategory.SkillCategoryId.ToString() },"/Admin/Footer/EditFooter");
        Assert.Equal(1,await db.Categories.CountAsync()); Assert.Equal(1,await db.SkillCategories.CountAsync());
        Assert.Equal(1,await db.Projects.CountAsync()); Assert.Equal(1,await db.Skills.CountAsync());
    }
}
