
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SocialMediaController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ILogger<SocialMediaController> _logger;

        public SocialMediaController(
            PortfolioContext context,
            ILogger<SocialMediaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== LIST ====================

        [HttpGet]
        public async Task<IActionResult> SocialMediaList()
        {
            var socialMedias = await _context.SocialMedias
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.SocialMediaId)
                .ToListAsync();

            return View(socialMedias);
        }

        // ==================== CREATE - GET ====================

        [HttpGet]
        public IActionResult CreateSocialMedia()
        {
            var model = new SocialMediaFormViewModel
            {
                IsActive = true,
                DisplayOrder = 0
            };

            return View(model);
        }

        // ==================== CREATE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSocialMedia(
            SocialMediaFormViewModel model)
        {
            ValidateSocialMedia(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var socialMedia = new SocialMedia
            {
                Platform = model.Platform.Trim(),
                Url = model.Url.Trim(),
                IconClass = model.IconClass?.Trim(),
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive
            };

            try
            {
                _context.SocialMedias.Add(socialMedia);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Sosyal medya hesabı başarıyla eklendi.";

                return RedirectToAction(nameof(SocialMediaList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Sosyal medya hesabı eklenemedi.");

                ModelState.AddModelError(
                    string.Empty,
                    "Sosyal medya hesabı kaydedilirken hata oluştu.");

                return View(model);
            }
        }

        // ==================== UPDATE - GET ====================

        [HttpGet]
        public async Task<IActionResult> UpdateSocialMedia(int id)
        {
            var socialMedia = await _context.SocialMedias
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.SocialMediaId == id);

            if (socialMedia == null)
            {
                return NotFound();
            }

            var model = new SocialMediaFormViewModel
            {
                SocialMediaId = socialMedia.SocialMediaId,
                Platform = socialMedia.Platform,
                Url = socialMedia.Url,
                IconClass = socialMedia.IconClass,
                DisplayOrder = socialMedia.DisplayOrder,
                IsActive = socialMedia.IsActive
            };

            return View(model);
        }

        // ==================== UPDATE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSocialMedia(
            SocialMediaFormViewModel model)
        {
            var socialMedia = await _context.SocialMedias
                .FirstOrDefaultAsync(
                    x => x.SocialMediaId == model.SocialMediaId);

            if (socialMedia == null)
            {
                return NotFound();
            }

            ValidateSocialMedia(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            socialMedia.Platform = model.Platform.Trim();
            socialMedia.Url = model.Url.Trim();
            socialMedia.IconClass = model.IconClass?.Trim();
            socialMedia.DisplayOrder = model.DisplayOrder;
            socialMedia.IsActive = model.IsActive;

            try
            {
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Sosyal medya hesabı başarıyla güncellendi.";

                return RedirectToAction(nameof(SocialMediaList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Sosyal medya hesabı güncellenemedi. Id: {Id}",
                    model.SocialMediaId);

                ModelState.AddModelError(
                    string.Empty,
                    "Sosyal medya hesabı güncellenirken hata oluştu.");

                return View(model);
            }
        }

        // ==================== DELETE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSocialMedia(int id)
        {
            var socialMedia = await _context.SocialMedias
                .FirstOrDefaultAsync(
                    x => x.SocialMediaId == id);

            if (socialMedia == null)
            {
                return NotFound();
            }

            try
            {
                _context.SocialMedias.Remove(socialMedia);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Sosyal medya hesabı başarıyla silindi.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Sosyal medya hesabı silinemedi. Id: {Id}",
                    id);

                TempData["ErrorMessage"] =
                    "Sosyal medya hesabı silinirken hata oluştu.";
            }

            return RedirectToAction(nameof(SocialMediaList));
        }

        // ==================== VALIDATION ====================

        private void ValidateSocialMedia(
            SocialMediaFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Platform))
            {
                ModelState.AddModelError(
                    nameof(model.Platform),
                    "Platform adı boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.Url))
            {
                ModelState.AddModelError(
                    nameof(model.Url),
                    "Profil bağlantısı boş olamaz.");

                return;
            }

            // Yalnızca HTTP ve HTTPS bağlantılarına izin ver.
            if (!Uri.TryCreate(
                    model.Url.Trim(),
                    UriKind.Absolute,
                    out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps &&
                 uri.Scheme != Uri.UriSchemeHttp))
            {
                ModelState.AddModelError(
                    nameof(model.Url),
                    "Bağlantı http:// veya https:// ile başlamalıdır.");
            }
        }
    }
}
