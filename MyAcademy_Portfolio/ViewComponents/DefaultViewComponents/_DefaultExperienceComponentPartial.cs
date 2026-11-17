using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;

namespace MyAcademy_PortfolioProject.ViewComponents.DefaultViewComponents
{
    public class _DefaultExperienceComponentPartial : ViewComponent
    {
        private readonly PortfolioContext portfolioContext;

        public _DefaultExperienceComponentPartial(PortfolioContext portfolioContext)
        {
            this.portfolioContext = portfolioContext;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var values = await portfolioContext.Experiences
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.ExperienceId)
                .ToListAsync();

            return View(values);
        }
    }
}