using Microsoft.AspNetCore.Mvc;
using MyAcademy_PortfolioProject.Infrastructure;

namespace MyAcademy_PortfolioProject.ViewComponents.DefaultViewComponents;

public class _DefaultHeaderComponentPartial(BrandingProvider branding) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync() => View(await branding.GetAsync());
}