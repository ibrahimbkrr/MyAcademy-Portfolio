using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Infrastructure;
using SkiaSharp;

namespace MyAcademy_PortfolioProject.Tests;

public partial class IntegrationTests
{
    [Fact]
    public async Task Rate_limit_is_partitioned_by_connection_peer()
    {
        async Task<int> Send(string peer) => (await factory.Server.SendAsync(context => {
            context.Connection.RemoteIpAddress=IPAddress.Parse(peer);
            context.Request.Scheme="https"; context.Request.Method="POST"; context.Request.Path="/Contact/Send";
        })).Response.StatusCode;
        for(int i=0;i<5;i++) Assert.Equal(400,await Send("192.0.2.1")); // CSRF is still required.
        Assert.Equal(429,await Send("192.0.2.1"));
        Assert.Equal(400,await Send("192.0.2.2"));
    }

    [Fact]
    public async Task Expired_cookie_cannot_access_admin()
    {
        await CreateUser();
        using var scope=factory.Services.CreateScope();
        var users=scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MyAcademy_PortfolioProject.Entities.AppUser>>();
        var signIn=scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.SignInManager<MyAcademy_PortfolioProject.Entities.AppUser>>();
        var principal=await signIn.CreateUserPrincipalAsync((await users.FindByEmailAsync("admin@example.test"))!);
        var options=scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>>().Get(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
        var properties=new Microsoft.AspNetCore.Authentication.AuthenticationProperties { IssuedUtc=DateTimeOffset.UtcNow.AddHours(-3),ExpiresUtc=DateTimeOffset.UtcNow.AddHours(-1) };
        var ticket=new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal,properties,Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
        using var expired=factory.NewClient(); expired.DefaultRequestHeaders.Add("Cookie",options.Cookie.Name+"="+options.TicketDataFormat.Protect(ticket));
        var response=await expired.GetAsync("/Admin/Dashboard/Index"); Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        Assert.Contains("/Admin/Account/Login",response.Headers.Location!.ToString());
    }

    [Theory]
    [InlineData("jpg",SKEncodedImageFormat.Jpeg)]
    [InlineData("png",SKEncodedImageFormat.Png)]
    [InlineData("webp",SKEncodedImageFormat.Webp)]
    public async Task Supported_image_formats_decode_and_reencode(string extension,SKEncodedImageFormat format)
    {
        using var bitmap=new SKBitmap(3,2); bitmap.Erase(SKColors.Blue);
        using var image=SKImage.FromBitmap(bitmap); using var encoded=image.Encode(format,90);
        var file=Upload(encoded.ToArray(),"photo."+extension);
        Assert.Null(await UploadSafety.ValidateImageAsync(file));
        using var output=new MemoryStream(); await UploadSafety.CopySafeImageAsync(file,output);
        Assert.Null(await UploadSafety.ValidateImageAsync(Upload(output.ToArray(),"photo."+extension)));
    }

    [Fact]
    public async Task Production_does_not_seed_even_with_configured_credentials()
    {
        using var production=new PortfolioFactory("Production",new Dictionary<string,string?> {
            ["AdminSeed:Email"]="not-created@example.test",["AdminSeed:Password"]="Aa1!"+Guid.NewGuid().ToString("N") });
        using var http=production.NewClient();
        Assert.Equal(HttpStatusCode.OK,(await http.GetAsync("/Admin/Account/Login")).StatusCode);
        using var scope=production.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        Assert.Equal(0,await db.Users.CountAsync()); Assert.Equal(0,await db.Roles.CountAsync());
    }

    [Fact]
    public async Task Uploaded_pdf_is_download_only_and_readiness_has_no_sensitive_details()
    {
        var root=Path.Combine(AppContext.BaseDirectory,"upload-results",Guid.NewGuid().ToString("N"));
        var directory=Path.Combine(root,"files","cv"); Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,"test.pdf"); await File.WriteAllTextAsync(path,"%PDF-1.4\n%%EOF");
        using var isolated=new PortfolioFactory(settings:new Dictionary<string,string?> { ["Uploads:RootDirectory"]=root });
        using var http=isolated.NewClient();
        var response=await http.GetAsync("/files/cv/test.pdf");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Equal("attachment",response.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Contains("sandbox",response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff",response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("Healthy",await http.GetStringAsync("/health/ready"));
        File.Delete(path);
    }

    [Fact]
    public async Task Login_ignores_external_return_url_and_cookie_has_bounded_expiry()
    {
        await CreateUser();
        var response=await Post("/Admin/Account/Login?ReturnUrl=https://evil.example",new() { ["Email"]="admin@example.test",["Password"]=password });
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        Assert.StartsWith("/Admin",response.Headers.Location!.ToString());
        var options=factory.Services.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>>()
            .Get(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
        Assert.Equal(TimeSpan.FromHours(2),options.ExpireTimeSpan);
        Assert.True(options.SlidingExpiration);
    }

    [Fact]
    public async Task Upload_replacement_cleans_failed_files_and_preserves_committed_files()
    {
        await Admin();
        var environment=factory.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var root=Path.Combine(AppContext.BaseDirectory,"upload-results",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); environment.WebRootPath=root;
        async Task<HttpResponseMessage> Send()
        {
            using var form=new MultipartFormDataContent();
            form.Add(new StringContent(await Token("/Admin/About/EditAbout")),"__RequestVerificationToken");
            form.Add(new StringContent("Test"),"Name"); form.Add(new StringContent("Test"),"Title");
            form.Add(new ByteArrayContent(ValidPng()),"ImageFile","../../same.png");
            return await client.PostAsync("/Admin/About/EditAbout",form);
        }
        Assert.Equal(HttpStatusCode.Redirect,(await Send()).StatusCode);
        var original=Directory.GetFiles(root,"*",SearchOption.AllDirectories).Single();
        factory.SaveFailures.FailAboutWrites=true;
        Assert.Equal(HttpStatusCode.OK,(await Send()).StatusCode);
        Assert.Equal(original,Directory.GetFiles(root,"*",SearchOption.AllDirectories).Single());
        using(var scope=factory.Services.CreateScope())
        {
            var about=await scope.ServiceProvider.GetRequiredService<PortfolioContext>().Abouts.SingleAsync();
            Assert.EndsWith(Path.GetFileName(original),about.ImageUrl);
        }
        factory.SaveFailures.FailAboutWrites=false;
        Assert.Equal(HttpStatusCode.Redirect,(await Send()).StatusCode);
        var replacement=Directory.GetFiles(root,"*",SearchOption.AllDirectories).Single();
        Assert.NotEqual(original,replacement); Assert.False(File.Exists(original));
        File.Delete(replacement);
    }

    [Fact]
    public async Task Contact_rate_limit_rejects_abuse_and_cannot_be_bypassed_with_forwarded_headers()
    {
        var token=await Token("/");
        for(int i=0;i<7;i++)
        {
            using var request=new HttpRequestMessage(HttpMethod.Post,"/Contact/Send");
            request.Headers.Add("X-Forwarded-For",$"203.0.113.{i+1}");
            request.Content=new FormUrlEncodedContent(new Dictionary<string,string> {
                ["FullName"]="Rate audit",["Email"]="audit@example.test",["Content"]="Test",["IsRead"]="true",["__RequestVerificationToken"]=token });
            var response=await client.SendAsync(request);
            Assert.Equal(i<5 ? HttpStatusCode.Redirect : HttpStatusCode.TooManyRequests,response.StatusCode);
            if(i>=5) Assert.NotNull(response.Headers.RetryAfter);
        }
        using var scope=factory.Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        Assert.Equal(5,await db.Messages.CountAsync()); Assert.False(await db.Messages.AnyAsync(x=>x.IsRead));
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/")).StatusCode);
        await Admin();
        Assert.Equal(HttpStatusCode.Redirect,(await Post("/Admin/Footer/EditFooter",new() { ["BrandName"]="Unaffected" },"/Admin/Footer/EditFooter")).StatusCode);
    }

    [Fact]
    public async Task Empty_frontend_has_no_empty_images_or_empty_mail_link()
    {
        var html=await client.GetStringAsync("/");
        Assert.DoesNotContain("<img",html);
        Assert.DoesNotContain("href=\"mailto:\"",html);
        Assert.Contains("<title>Portfolyo</title>",html);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/health/live")).StatusCode);
    }

    [Fact]
    public async Task Branding_uses_existing_footer_and_about_and_encodes_text()
    {
        await Admin();
        await Post("/Admin/Footer/EditFooter",new() { ["BrandName"]="<b>Brand</b>" },"/Admin/Footer/EditFooter");
        await Post("/Admin/About/EditAbout",new() { ["Name"]="Name",["Title"]="About",["JobTitle"]="Engineer" },"/Admin/About/EditAbout");
        var html=await client.GetStringAsync("/");
        Assert.Contains("&lt;b&gt;Brand&lt;/b&gt;",html); Assert.Contains("Engineer</title>",html);
        Assert.DoesNotContain("<b>Brand</b>",html);
    }

    private static byte[] ValidPng()
    {
        using var bitmap=new SKBitmap(2,2); bitmap.Erase(SKColors.Green);
        using var image=SKImage.FromBitmap(bitmap); using var data=image.Encode(SKEncodedImageFormat.Png,100);
        return data.ToArray();
    }

    private static IFormFile Upload(byte[] bytes,string name,string mime="application/octet-stream") =>
        new FormFile(new MemoryStream(bytes),0,bytes.Length,"ImageFile",name) { Headers=new HeaderDictionary(),ContentType=mime };

    [Theory]
    [InlineData("empty")]
    [InlineData("oversize")]
    [InlineData("signature")]
    [InlineData("mismatch")]
    [InlineData("mime")]
    [InlineData("truncated")]
    [InlineData("executable")]
    public async Task Image_validation_rejects_invalid_content(string scenario)
    {
        var bytes=ValidPng(); var name="image.png"; var mime="image/png";
        switch(scenario)
        {
            case "empty": bytes=[]; break;
            case "oversize": bytes=new byte[UploadSafety.ImageLimit+1]; break;
            case "signature": bytes="not an image"u8.ToArray(); break;
            case "mismatch": name="image.jpg"; mime="image/jpeg"; break;
            case "mime": mime="text/html"; break;
            case "truncated": bytes=bytes[..24]; break;
            case "executable": name="image.aspx"; break;
        }
        Assert.NotNull(await UploadSafety.ValidateImageAsync(Upload(bytes,name,mime)));
    }

    [Fact]
    public async Task Image_reencoding_discards_trailing_content()
    {
        var bytes=ValidPng().Concat("<script>unexpected-payload</script>"u8.ToArray()).ToArray();
        var file=Upload(bytes,"../../safe.png","image/png");
        Assert.Null(await UploadSafety.ValidateImageAsync(file));
        using var output=new MemoryStream(); await UploadSafety.CopySafeImageAsync(file,output);
        Assert.DoesNotContain("unexpected-payload",System.Text.Encoding.ASCII.GetString(output.ToArray()));
        Assert.Null(await UploadSafety.ValidateImageAsync(Upload(output.ToArray(),"safe.png","image/png")));
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("oversize")]
    [InlineData("signature")]
    [InlineData("missing-eof")]
    [InlineData("mime")]
    public async Task Pdf_validation_rejects_invalid_content(string scenario)
    {
        var bytes="%PDF-1.4\n%%EOF"u8.ToArray(); var mime="application/pdf";
        switch(scenario)
        {
            case "empty": bytes=[]; break;
            case "oversize": bytes=new byte[UploadSafety.PdfLimit+1]; break;
            case "signature": bytes="invalid\n%%EOF"u8.ToArray(); break;
            case "missing-eof": bytes="%PDF-1.4\ntruncated"u8.ToArray(); break;
            case "mime": mime="application/javascript"; break;
        }
        Assert.NotNull(await UploadSafety.ValidatePdfAsync(Upload(bytes,"file.pdf",mime)));
    }
}
