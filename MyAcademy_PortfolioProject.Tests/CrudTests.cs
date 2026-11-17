using System.Net;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Tests;

public partial class IntegrationTests
{
    public static IEnumerable<object[]> CrudCases()
    {
        yield return new object[] { "SocialMedia", "SocialMedia", "SocialMedias", "SocialMediaId", "Platform", new Dictionary<string,string> { ["Platform"]="Audit", ["Url"]="https://example.test", ["IsActive"]="true" } };
        yield return new object[] { "Category", "Category", "Categories", "CategoryId", "CategoryName", new Dictionary<string,string> { ["CategoryName"]="Audit", ["FilterKey"]="audit" } };
        yield return new object[] { "SkillCategory", "Category", "SkillCategories", "SkillCategoryId", "CategoryName", new Dictionary<string,string> { ["CategoryName"]="Audit" } };
        yield return new object[] { "HeroTitle", "HeroTitle", "HeroTitles", "HeroTitleId", "Title", new Dictionary<string,string> { ["Title"]="Audit", ["Order"]="1", ["IsActive"]="true" } };
        yield return new object[] { "Experience", "Experience", "Experiences", "ExperienceId", "Title", new Dictionary<string,string> { ["Title"]="Audit", ["CompanyName"]="Test", ["Description"]="Test", ["CategoryName"]="Work", ["IsActive"]="true" } };
        yield return new object[] { "Education", "Education", "Educations", "EducationId", "Title", new Dictionary<string,string> { ["Title"]="Audit", ["Institution"]="Test", ["EducationType"]="Course", ["IsActive"]="true" } };
        yield return new object[] { "Message", "Message", "Messages", "MessageId", "SenderName", new Dictionary<string,string> { ["SenderName"]="Audit", ["SenderEmail"]="sender@example.test", ["MessageContent"]="Test" } };
        yield return new object[] { "Project", "Project", "Projects", "ProjectId", "ProjectTitle", new Dictionary<string,string> { ["ProjectTitle"]="Audit", ["Description"]="Test", ["IsActive"]="true" } };
        yield return new object[] { "Skill", "Skill", "Skills", "SkillId", "SkillName", new Dictionary<string,string> { ["SkillName"]="Audit", ["Level"]="50" } };
    }

    [Theory, MemberData(nameof(CrudCases))]
    public async Task Crud_persists_updates_deletes_and_handles_missing_ids(string controller, string action, string table, string idField, string field, Dictionary<string,string> values)
    {
        await Admin();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        if (controller == "Project")
        {
            var category = new Category { CategoryName="Test", FilterKey="test" }; db.Categories.Add(category); await db.SaveChangesAsync(); values["CategoryId"] = category.CategoryId.ToString();
        }
        if (controller == "Skill")
        {
            var category = new SkillCategory { CategoryName="Test" }; db.SkillCategories.Add(category); await db.SaveChangesAsync(); values["SkillCategoryId"] = category.SkillCategoryId.ToString();
        }
        var prefix = "/Admin/" + controller + "/";
        var create = prefix + "Create" + action;
        Assert.Equal(HttpStatusCode.Redirect,(await Post(create,values,create)).StatusCode);
        // Identifiers are constants from the test cases, never request data.
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT MAX({idField}) FROM {table}";
        var id = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.True(id > 0);
        var update = prefix + "Update" + action;
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync(update + "/" + id)).StatusCode);
        values[idField] = id.ToString(); values[field] = "UpdatedAudit";
        Assert.Equal(HttpStatusCode.Redirect,(await Post(update,values,update+"/"+id)).StatusCode);
        command.CommandText = $"SELECT {field} FROM {table} WHERE {idField}={id}";
        Assert.Equal("UpdatedAudit",await command.ExecuteScalarAsync());
        values[field] = "";
        Assert.Equal(HttpStatusCode.OK,(await Post(update,values,update+"/"+id)).StatusCode);
        Assert.Equal("UpdatedAudit",await command.ExecuteScalarAsync());
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync(update+"/2147483647")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await Post(prefix+"Delete"+action,new() { ["id"]=id.ToString() },update+"/"+id)).StatusCode);
        command.CommandText = $"SELECT COUNT(*) FROM {table} WHERE {idField}={id}";
        Assert.Equal(0L,await command.ExecuteScalarAsync());
        Assert.Equal(HttpStatusCode.NotFound,(await Post(prefix+"Delete"+action,new() { ["id"]=id.ToString() },"/Admin/Footer/EditFooter")).StatusCode);
    }

    [Fact]
    public async Task Every_admin_post_requires_authentication_and_antiforgery()
    {
        var actions = factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>().Where(x => x.RouteValues.TryGetValue("area", out var area) && area=="Admin" && x.MethodInfo.IsDefined(typeof(HttpPostAttribute),true) && x.ControllerName != "Account").ToList();
        Assert.NotEmpty(actions);
        foreach (var action in actions)
        {
            var path = $"/Admin/{action.ControllerName}/{action.ActionName}";
            Assert.Equal(HttpStatusCode.Redirect,(await client.PostAsync(path,new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
        }
        await Admin();
        foreach (var action in actions)
        {
            var path = $"/Admin/{action.ControllerName}/{action.ActionName}";
            Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsync(path,new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
        }
    }

    [Fact]
    public async Task Failed_password_attempts_lock_account()
    {
        await CreateUser();
        for(int i=0;i<5;i++) await Login(suppliedPassword:"wrong");
        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();
        Assert.True(await manager.IsLockedOutAsync((await manager.FindByEmailAsync("admin@example.test"))!));
        Assert.Equal(HttpStatusCode.OK,(await Login()).StatusCode);
    }

    [Fact]
    public async Task Contact_form_validates_and_prevents_overposting()
    {
        var response = await Post("/Contact/Send",new() { ["FullName"]="Audit", ["Email"]="test@example.test", ["Content"]="<script>alert(1)</script>", ["IsRead"]="true", ["MessageId"]="999" },"/Default/Index");
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        var message = await db.Messages.SingleAsync(); Assert.False(message.IsRead); Assert.NotEqual(999,message.MessageId);
        await Admin();
        var html = await client.GetStringAsync("/Admin/Message/MessageDetails/"+message.MessageId);
        Assert.DoesNotContain("<script>alert(1)</script>",html); Assert.Contains("&lt;script&gt;",html);
    }
}
