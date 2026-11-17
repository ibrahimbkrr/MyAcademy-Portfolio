
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [MyAcademy_PortfolioProject.Infrastructure.SerializedAdminWrite("Philosophy")]
    public class PhilosophyController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ILogger<PhilosophyController> _logger;

        public PhilosophyController(
            PortfolioContext context,
            ILogger<PhilosophyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== EDIT - GET ====================

        [HttpGet]
        public async Task<IActionResult> EditPhilosophy()
        {
            var philosophy = await _context.Philosophies
                .AsNoTracking()
                .OrderBy(x => x.PhilosophyId)
                .FirstOrDefaultAsync();

            var model = new PhilosophyFormViewModel();

            if (philosophy != null)
            {
                model.PhilosophyId = philosophy.PhilosophyId;
                model.Quote = philosophy.Quote ?? string.Empty;
                model.Author = philosophy.Author;
                model.Subtitle = philosophy.Subtitle;
            }

            return View(model);
        }

        // ==================== EDIT - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPhilosophy(
            PhilosophyFormViewModel model)
        {
            // Yalnızca boşluklardan oluşan metni engelle
            if (string.IsNullOrWhiteSpace(model.Quote))
            {
                ModelState.AddModelError(
                    nameof(model.Quote),
                    "Felsefe metni boş olamaz.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var philosophy = await _context.Philosophies
                    .OrderBy(x => x.PhilosophyId)
                    .FirstOrDefaultAsync();

                // İlk kayıt yoksa oluştur
                if (philosophy == null)
                {
                    philosophy = new Philosophy();
                    _context.Philosophies.Add(philosophy);
                }

                // Formdan gelen içerikleri aktar
                philosophy.Quote = model.Quote.Trim();
                philosophy.Author = model.Author?.Trim();
                philosophy.Subtitle = model.Subtitle?.Trim();

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Felsefe bölümü başarıyla güncellendi.";

                return RedirectToAction(nameof(EditPhilosophy));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Felsefe bölümü kaydedilemedi.");

                ModelState.AddModelError(
                    string.Empty,
                    "Bilgiler kaydedilirken bir hata oluştu.");

                return View(model);
            }
        }
    }
}
