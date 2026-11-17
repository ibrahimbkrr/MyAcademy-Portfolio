using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Models;

namespace MyAcademy_PortfolioProject.Infrastructure;

public sealed class BrandingProvider(PortfolioContext db)
{
    private Task<BrandingViewModel>? cached;
    public Task<BrandingViewModel> GetAsync() => cached ??= LoadAsync();
    private async Task<BrandingViewModel> LoadAsync()
    {
        var footer = await db.Footers.AsNoTracking().OrderBy(x=>x.FooterId).FirstOrDefaultAsync();
        var about = await db.Abouts.AsNoTracking().OrderBy(x=>x.AboutId).FirstOrDefaultAsync();
        return new BrandingViewModel {
            Name = !string.IsNullOrWhiteSpace(footer?.BrandName) ? footer.BrandName : !string.IsNullOrWhiteSpace(about?.Name) ? about.Name : "Portfolyo",
            JobTitle = about?.JobTitle
        };
    }
}
