
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HeroTitleController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ILogger<HeroTitleController> _logger;

        public HeroTitleController(
            PortfolioContext context,
            ILogger<HeroTitleController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== TITLE LIST ====================

        [HttpGet]
        public async Task<IActionResult> HeroTitleList()
        {
            var titles = await _context.HeroTitles
                .AsNoTracking()
                .OrderBy(x => x.Order)
                .ThenBy(x => x.HeroTitleId)
                .ToListAsync();

            return View(titles);
        }

        // ==================== CREATE - GET ====================

        [HttpGet]
        public IActionResult CreateHeroTitle()
        {
            return View(new HeroTitleFormViewModel
            {
                IsActive = true
            });
        }

        // ==================== CREATE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHeroTitle(
            HeroTitleFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Hareketli başlık boş olamaz.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var title = new HeroTitle
            {
                Title = model.Title.Trim(),
                Order = model.Order,
                IsActive = model.IsActive
            };

            try
            {
                _context.HeroTitles.Add(title);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Hareketli başlık başarıyla eklendi.";

                return RedirectToAction(nameof(HeroTitleList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Hareketli başlık eklenemedi.");

                ModelState.AddModelError(
                    string.Empty,
                    "Başlık kaydedilirken bir hata oluştu.");

                return View(model);
            }
        }

        // ==================== UPDATE - GET ====================

        [HttpGet]
        public async Task<IActionResult> UpdateHeroTitle(int id)
        {
            var title = await _context.HeroTitles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.HeroTitleId == id);

            if (title == null)
                return NotFound();

            var model = new HeroTitleFormViewModel
            {
                HeroTitleId = title.HeroTitleId,
                Title = title.Title,
                Order = title.Order,
                IsActive = title.IsActive
            };

            return View(model);
        }

        // ==================== UPDATE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateHeroTitle(
            HeroTitleFormViewModel model)
        {
            var title = await _context.HeroTitles
                .FirstOrDefaultAsync(x =>
                    x.HeroTitleId == model.HeroTitleId);

            if (title == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Hareketli başlık boş olamaz.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            title.Title = model.Title.Trim();
            title.Order = model.Order;
            title.IsActive = model.IsActive;

            try
            {
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Hareketli başlık başarıyla güncellendi.";

                return RedirectToAction(nameof(HeroTitleList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Hareketli başlık güncellenemedi. Id: {Id}",
                    model.HeroTitleId);

                ModelState.AddModelError(
                    string.Empty,
                    "Başlık güncellenirken bir hata oluştu.");

                return View(model);
            }
        }

        // ==================== DELETE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHeroTitle(int id)
        {
            var title = await _context.HeroTitles
                .FirstOrDefaultAsync(x => x.HeroTitleId == id);

            if (title == null)
                return NotFound();

            try
            {
                _context.HeroTitles.Remove(title);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Hareketli başlık başarıyla silindi.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Hareketli başlık silinemedi. Id: {Id}",
                    id);

                TempData["ErrorMessage"] =
                    "Başlık silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(HeroTitleList));
        }
    }
}
