using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.ViewComponents.DefaultViewComponents
{
    public class _DefaultAboutMeSectionComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultAboutMeSectionComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var value = await _context.Abouts
                .AsNoTracking()
                .OrderBy(x => x.AboutId)
                .FirstOrDefaultAsync();

            return View(value ?? new About());
        }
    }
}