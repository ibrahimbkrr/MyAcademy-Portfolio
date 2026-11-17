using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Models;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.ViewComponents.DefaultViewComponents
{
    public class _DefaultEducationComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultEducationComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var educations = await _context.Educations
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.EducationId)
                .ToListAsync();

            var educationContent = await _context.EducationContents
                .AsNoTracking()
                .OrderBy(x => x.EducationContentId)
                .FirstOrDefaultAsync();

            var model = new EducationViewModel
            {
                Educations = educations,
                EducationContent = educationContent ?? new EducationContent()
            };

            return View(model);
        }
    }
}