using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Models;

namespace MyAcademy_PortfolioProject.ViewComponents.DefaultViewComponents
{
    public class _DefaultProjectComponentPartial : ViewComponent
    {
        private readonly PortfolioContext _context;

        public _DefaultProjectComponentPartial(PortfolioContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {

            var projects = await _context.Projects
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.ProjectId)
                .ToListAsync();


            var categories = await _context.Categories
                .AsNoTracking()
                .Where(x => x.Projects.Any(p => p.IsActive))
                .OrderBy(x => x.CategoryName)
                .ToListAsync();


            var links = await _context.SocialMedias.AsNoTracking().Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder).ThenBy(x => x.SocialMediaId).ToListAsync();
            var model = new ProjectViewModel
            {
                AllProjectsUrl = links.Select(x => x.Url).FirstOrDefault(url =>
                    Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                    uri.Scheme == Uri.UriSchemeHttps && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)),
                Projects = projects,
                Categories = categories
            };

            return View(model);
        }
    }
}
