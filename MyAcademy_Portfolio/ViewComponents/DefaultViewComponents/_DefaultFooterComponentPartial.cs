using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;
using MyAcademy_PortfolioProject.Models;

namespace MyAcademy_PortfolioProject.ViewComponents.DefaultViewComponents
{
    public class _DefaultFooterComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultFooterComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var footer = await _context.Footers
                .AsNoTracking()
                .OrderBy(x => x.FooterId)
                .FirstOrDefaultAsync();

            var socialMedias = await _context.SocialMedias
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.SocialMediaId)
                .ToListAsync();

            var model = new FooterViewModel
            {
                Footer = footer ?? new Footer(),
                SocialMedias = socialMedias
            };

            return View(model);
        }
    }
}