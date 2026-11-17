
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [MyAcademy_PortfolioProject.Infrastructure.SerializedAdminWrite("Footer")]
    public class FooterController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ILogger<FooterController> _logger;

        public FooterController(
            PortfolioContext context,
            ILogger<FooterController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== EDIT FOOTER - GET ====================

        [HttpGet]
        public async Task<IActionResult> EditFooter()
        {
            var footer = await _context.Footers
                .AsNoTracking()
                .OrderBy(x => x.FooterId)
                .FirstOrDefaultAsync();

            var model = new FooterFormViewModel();

            if (footer != null)
            {
                model.FooterId = footer.FooterId;
                model.BrandName = footer.BrandName;
                model.Description = footer.Description;
                model.CopyrightText = footer.CopyrightText;
            }

            return View(model);
        }

        // ==================== EDIT FOOTER - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFooter(
            FooterFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.BrandName))
            {
                ModelState.AddModelError(
                    nameof(model.BrandName),
                    "Marka veya isim alanı boş olamaz.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Mevcut Footer kaydını getir
                var footer = await _context.Footers
                    .OrderBy(x => x.FooterId)
                    .FirstOrDefaultAsync();

                // Kayıt yoksa ilk kaydı oluştur
                if (footer == null)
                {
                    footer = new Footer();
                    _context.Footers.Add(footer);
                }

                // Form bilgilerini Entity'ye aktar
                footer.BrandName = model.BrandName.Trim();

                footer.Description =
                    string.IsNullOrWhiteSpace(model.Description)
                        ? null
                        : model.Description.Trim();

                footer.CopyrightText =
                    string.IsNullOrWhiteSpace(model.CopyrightText)
                        ? null
                        : model.CopyrightText.Trim();

                // SQL Server'a kaydet
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Footer bilgileri başarıyla güncellendi.";

                return RedirectToAction(nameof(EditFooter));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Footer bilgileri kaydedilemedi.");

                ModelState.AddModelError(
                    string.Empty,
                    "Footer bilgileri kaydedilirken bir hata oluştu.");

                return View(model);
            }
        }
    }
}
