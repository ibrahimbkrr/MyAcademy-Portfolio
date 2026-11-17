using MyAcademy_PortfolioProject.Infrastructure;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [RequestSizeLimit(UploadSafety.RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadSafety.RequestLimit)]
    [MyAcademy_PortfolioProject.Infrastructure.SerializedAdminWrite("HeroSection")]
    public class HeroSectionController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UploadStorage _storage;
        private readonly ILogger<HeroSectionController> _logger;

        private const long MaxImageSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public HeroSectionController(
            PortfolioContext context,
            IWebHostEnvironment environment,
            UploadStorage storage,
            ILogger<HeroSectionController> logger)
        {
            _context = context;
            _environment = environment;
            _storage = storage;
            _logger = logger;
        }

        // ==================== EDIT HERO - GET ====================

        [HttpGet]
        public async Task<IActionResult> EditHero()
        {
            var hero = await _context.HeroSections
                .AsNoTracking()
                .OrderBy(x => x.HeroSectionId)
                .FirstOrDefaultAsync();

            var model = new HeroSectionFormViewModel();

            if (hero != null)
            {
                model.HeroSectionId = hero.HeroSectionId;
                model.Greeting = hero.Greeting ?? "";
                model.Title = hero.Title ?? "";
                model.Description = hero.Description ?? "";
                model.ExistingImageUrl = hero.MainImageUrl;

                model.TopBadgeValue = hero.TopBadgeValue;
                model.TopBadgeText = hero.TopBadgeText;

                model.BottomBadgeValue = hero.BottomBadgeValue;
                model.BottomBadgeText = hero.BottomBadgeText;
            }

            return View(model);
        }

        // ==================== EDIT HERO - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHero(
            HeroSectionFormViewModel model)
        {
            // Metin doğrulaması
            if (string.IsNullOrWhiteSpace(model.Greeting))
            {
                ModelState.AddModelError(
                    nameof(model.Greeting),
                    "Karşılama metni boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Ana başlık boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Açıklama boş olamaz.");
            }

            // Görsel doğrulaması
            await ValidateImageAsync(model.ImageFile);

            // Mevcut kayıt
            var hero = await _context.HeroSections
                .OrderBy(x => x.HeroSectionId)
                .FirstOrDefaultAsync();

            if (!ModelState.IsValid)
            {
                model.ExistingImageUrl = hero?.MainImageUrl;
                return View(model);
            }

            var oldImageUrl = hero?.MainImageUrl;
            string? newImageUrl = null;

            try
            {
                // Yeni görsel varsa yükle
                if (model.ImageFile != null)
                {
                    newImageUrl =
                        await SaveImageAsync(model.ImageFile);
                }

                // İlk kayıt yoksa oluştur
                if (hero == null)
                {
                    hero = new HeroSection();
                    _context.HeroSections.Add(hero);
                }

                // Hero metinlerini güncelle
                hero.Greeting = model.Greeting.Trim();
                hero.Title = model.Title.Trim();
                hero.Description = model.Description.Trim();

                // Rozet bilgileri
                hero.TopBadgeValue = model.TopBadgeValue?.Trim();
                hero.TopBadgeText = model.TopBadgeText?.Trim();

                hero.BottomBadgeValue =
                    model.BottomBadgeValue?.Trim();

                hero.BottomBadgeText =
                    model.BottomBadgeText?.Trim();

                // Yeni görsel yoksa eski görsel korunur
                if (newImageUrl != null)
                {
                    hero.MainImageUrl = newImageUrl;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Hero bölümü güncellenemedi.");

                // Kayıt başarısızsa yeni görseli temizle
                DeleteManagedImage(newImageUrl);

                model.ExistingImageUrl = oldImageUrl;

                ModelState.AddModelError(
                    string.Empty,
                    "Hero bilgileri kaydedilirken bir hata oluştu.");

                return View(model);
            }

            // Başarılı kayıt sonrası eski yüklenen görseli temizle
            if (newImageUrl != null)
            {
                DeleteManagedImage(oldImageUrl);
            }

            TempData["SuccessMessage"] =
                "Hero bölümü başarıyla güncellendi.";

            return RedirectToAction(nameof(EditHero));
        }

        // ==================== IMAGE VALIDATION ====================

        private async Task ValidateImageAsync(IFormFile? image)
        {
            var error = await UploadSafety.ValidateImageAsync(image);
            if (error != null) ModelState.AddModelError("ImageFile", error);
        }

        // ==================== SAVE IMAGE ====================

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var webRoot = _storage.Root;

            var folderPath = Path.Combine(
                webRoot,
                "images",
                "hero");

            Directory.CreateDirectory(folderPath);

            var extension = Path.GetExtension(
                image.FileName).ToLowerInvariant();

            var fileName = $"{Guid.NewGuid():N}{extension}";

            var filePath = Path.Combine(
                folderPath,
                fileName);

            try
            {
                await using var stream = new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write);

                await UploadSafety.CopySafeImageAsync(image, stream);
            }
            catch
            {
                try
                {
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                catch (Exception ex) when (
                    ex is IOException ||
                    ex is UnauthorizedAccessException)
                {
                    _logger.LogWarning(
                        ex,
                        "Kısmi Hero görseli temizlenemedi.");
                }

                throw;
            }

            return $"/images/hero/{fileName}";
        }

        // ==================== DELETE MANAGED IMAGE ====================

        private void DeleteManagedImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            const string prefix = "/images/hero/";

            if (!imageUrl.StartsWith(
                prefix,
                StringComparison.Ordinal))
            {
                return;
            }

            var fileName = imageUrl.Substring(prefix.Length);

            // Yol geçişini engelle
            if (fileName != Path.GetFileName(fileName))
                return;

            var extension = Path.GetExtension(fileName);

            if (!AllowedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            // Sadece GUID adlı, uygulamanın yüklediği dosyaları sil
            var name = Path.GetFileNameWithoutExtension(fileName);

            if (!Guid.TryParseExact(name, "N", out _))
                return;

            var webRoot = _storage.Root;

            var filePath = Path.Combine(
                webRoot,
                "images",
                "hero",
                fileName);

            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                _logger.LogWarning(
                    ex,
                    "Eski Hero görseli silinemedi: {FileName}",
                    fileName);
            }
        }
    }
}
