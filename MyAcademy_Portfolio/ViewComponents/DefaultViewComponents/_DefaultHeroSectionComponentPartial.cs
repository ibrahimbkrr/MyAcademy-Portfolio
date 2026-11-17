using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Models;

namespace MyAcademy_PortfolioProject.ViewComponents.DefaultViewComponents
{
    public class _DefaultHeroSectionComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultHeroSectionComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var value = await _context.HeroSections
                .AsNoTracking()
                .OrderBy(x => x.HeroSectionId)
                .FirstOrDefaultAsync();

            var socialMedias = await _context.SocialMedias
                .AsNoTracking()
                .Where(sm => sm.IsActive)
                .OrderBy(sm => sm.DisplayOrder)
                .ThenBy(sm => sm.SocialMediaId)
                .ToListAsync();

            var heroTitles = await _context.HeroTitles
                .AsNoTracking()
                .Where(ht => ht.IsActive)
                .OrderBy(ht => ht.Order)
                .ThenBy(ht => ht.HeroTitleId)
                .Select(ht => ht.Title)
                .ToListAsync();

            var model = new HeroSectionViewModel
            {
                Greeting = value?.Greeting,
                Title = value?.Title,
                Description = value?.Description,
                MainImageUrl = value?.MainImageUrl,

                BottomBadgeValue = value?.BottomBadgeValue,
                BottomBadgeText = value?.BottomBadgeText,
                TopBadgeValue = value?.TopBadgeValue,
                TopBadgeText = value?.TopBadgeText,

                SocialMedias = socialMedias,
                HeroTitles = heroTitles
            };

            return View(model);
        }
    }
}