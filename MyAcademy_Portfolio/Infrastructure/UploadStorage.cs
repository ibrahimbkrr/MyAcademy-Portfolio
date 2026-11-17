namespace MyAcademy_PortfolioProject.Infrastructure;

public sealed class UploadStorage(IWebHostEnvironment environment,IConfiguration configuration)
{
    public string Root => configuration["Uploads:RootDirectory"] is { Length:>0 } root
        ? Path.GetFullPath(root)
        : environment.WebRootPath ?? Path.Combine(environment.ContentRootPath,"wwwroot");
}
