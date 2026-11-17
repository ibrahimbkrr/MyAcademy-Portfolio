using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Models;

namespace MyAcademy_PortfolioProject.ViewComponents.DefaultViewComponents
{
    public class _DefaultContactComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultContactComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var contact = await _context.Contacts
                .AsNoTracking()
                .OrderBy(x => x.ContactId)
                .FirstOrDefaultAsync();

            var socialMedias = await _context.SocialMedias
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.SocialMediaId)
                .ToListAsync();

            var model = new ContactViewModel
            {
                Contact = contact ?? new Entities.Contact(),
                SocialMedias = socialMedias
            };

            return View(model);
        }
    }
}