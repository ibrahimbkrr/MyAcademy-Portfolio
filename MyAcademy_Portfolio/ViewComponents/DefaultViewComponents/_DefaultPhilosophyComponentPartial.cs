using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.ViewComponents.DefaultViewComponents
{
    public class _DefaultPhilosophyComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultPhilosophyComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var value = await _context.Philosophies
                .AsNoTracking()
                .OrderBy(x => x.PhilosophyId)
                .FirstOrDefaultAsync();

            return View(value ?? new Philosophy());
        }
    }
}