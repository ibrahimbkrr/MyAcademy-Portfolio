
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [MyAcademy_PortfolioProject.Infrastructure.SerializedAdminWrite("EducationContent")]
    public class EducationContentController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ILogger<EducationContentController> _logger;

        public EducationContentController(
            PortfolioContext context,
            ILogger<EducationContentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== EDIT CONTENT - GET ====================

        [HttpGet]
        public async Task<IActionResult> EditContent()
        {
            var content = await _context.EducationContents
                .AsNoTracking()
                .OrderBy(x => x.EducationContentId)
                .FirstOrDefaultAsync();

            var model = new EducationContentFormViewModel();

            if (content != null)
            {
                model.EducationContentId = content.EducationContentId;
                model.SectionLabel = content.SectionLabel;
                model.SectionTitle = content.SectionTitle ?? "";
                model.Quote = content.Quote;
                model.Description = content.Description;
            }

            return View(model);
        }

        // ==================== EDIT CONTENT - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditContent(
            EducationContentFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.SectionTitle))
            {
                ModelState.AddModelError(
                    nameof(model.SectionTitle),
                    "Bölüm başlığı boş olamaz.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Mevcut tek içerik kaydını getir
                var content = await _context.EducationContents
                    .OrderBy(x => x.EducationContentId)
                    .FirstOrDefaultAsync();

                // Henüz kayıt yoksa oluştur
                if (content == null)
                {
                    content = new EducationContent();

                    _context.EducationContents.Add(content);
                }

                // Form bilgilerini aktar
                content.SectionLabel = model.SectionLabel?.Trim();
                content.SectionTitle = model.SectionTitle.Trim();
                content.Quote = model.Quote?.Trim();
                content.Description = model.Description?.Trim();

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Eğitim bölümü içeriği başarıyla kaydedildi.";

                return RedirectToAction(nameof(EditContent));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Eğitim bölümü içeriği kaydedilemedi.");

                ModelState.AddModelError(
                    string.Empty,
                    "İçerik kaydedilirken bir hata oluştu.");

                return View(model);
            }
        }
    }
}
