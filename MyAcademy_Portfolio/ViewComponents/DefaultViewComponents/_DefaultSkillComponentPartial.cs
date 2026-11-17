using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.ViewComponents.DefaultViewComponents
{
    public class _DefaultSkillComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultSkillComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.SkillCategories
                .AsNoTracking()
                .Include(x => x.Skills)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.SkillCategoryId)
                .ToListAsync();

            foreach (var category in categories)
            {
                category.Skills = category.Skills
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.SkillId)
                    .ToList();
            }

            return View(categories);
        }
    }
}