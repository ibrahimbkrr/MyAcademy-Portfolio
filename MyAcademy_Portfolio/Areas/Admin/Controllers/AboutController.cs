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
    [MyAcademy_PortfolioProject.Infrastructure.SerializedAdminWrite("About")]
    public class AboutController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UploadStorage _storage;
        private readonly ILogger<AboutController> _logger;




        private static readonly string[] ImageExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public AboutController(
            PortfolioContext context,
            IWebHostEnvironment environment,
            UploadStorage storage,
            ILogger<AboutController> logger)
        {
            _context = context;
            _environment = environment;
            _storage = storage;
            _logger = logger;
        }

        // ==================== EDIT ABOUT - GET ====================

        [HttpGet]
        public async Task<IActionResult> EditAbout()
        {
            var about = await _context.Abouts
                .AsNoTracking()
                .OrderBy(x => x.AboutId)
                .FirstOrDefaultAsync();

            var model = new AboutFormViewModel();

            if (about != null)
            {
                model.AboutId = about.AboutId;
                model.Name = about.Name ?? string.Empty;
                model.Location = about.Location;
                model.JobTitle = about.JobTitle;
                model.SectionLabel = about.SectionLabel;
                model.Title = about.Title ?? string.Empty;
                model.Description1 = about.Description1;
                model.Description2 = about.Description2;

                model.Stat1Value = about.Stat1Value;
                model.Stat1Label = about.Stat1Label;

                model.Stat2Value = about.Stat2Value;
                model.Stat2Label = about.Stat2Label;

                model.Stat3Value = about.Stat3Value;
                model.Stat3Label = about.Stat3Label;

                model.ExistingImageUrl = about.ImageUrl;
                model.ExistingCvUrl = about.CvUrl;
            }

            return View(model);
        }

        // ==================== EDIT ABOUT - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAbout(
            AboutFormViewModel model)
        {
            // 1. Zorunlu metinleri kontrol et
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Ad soyad boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Bölüm başlığı boş olamaz.");
            }

            // 2. Görsel ve CV dosyalarını kontrol et
            await ValidateImageAsync(model.ImageFile);
            await ValidateCvAsync(model.CvFile);

            // 3. Mevcut kayıt
            var about = await _context.Abouts
                .OrderBy(x => x.AboutId)
                .FirstOrDefaultAsync();

            if (!ModelState.IsValid)
            {
                SetExistingFiles(model, about);
                return View(model);
            }

            string? newImageUrl = null;
            string? newCvUrl = null;

            var oldImageUrl = about?.ImageUrl;
            var oldCvUrl = about?.CvUrl;

            try
            {
                // 4. Yeni fotoğraf seçildiyse yükle
                if (model.ImageFile != null)
                {
                    newImageUrl = await SaveFileAsync(
                        model.ImageFile,
                        "images",
                        "about");
                }

                // 5. Yeni CV seçildiyse yükle
                if (model.CvFile != null)
                {
                    newCvUrl = await SaveFileAsync(
                        model.CvFile,
                        "files",
                        "cv");
                }

                // 6. İlk kayıt yoksa oluştur
                if (about == null)
                {
                    about = new About();
                    _context.Abouts.Add(about);
                }

                // 7. Formdaki metin bilgilerini aktar
                about.Name = model.Name.Trim();
                about.Location = model.Location?.Trim();
                about.JobTitle = model.JobTitle?.Trim();

                about.SectionLabel = model.SectionLabel?.Trim();
                about.Title = model.Title.Trim();

                about.Description1 = model.Description1?.Trim();
                about.Description2 = model.Description2?.Trim();

                about.Stat1Value = model.Stat1Value?.Trim();
                about.Stat1Label = model.Stat1Label?.Trim();

                about.Stat2Value = model.Stat2Value?.Trim();
                about.Stat2Label = model.Stat2Label?.Trim();

                about.Stat3Value = model.Stat3Value?.Trim();
                about.Stat3Label = model.Stat3Label?.Trim();

                // Yeni dosya yoksa mevcut dosya adresi korunur
                if (newImageUrl != null)
                {
                    about.ImageUrl = newImageUrl;
                }

                if (newCvUrl != null)
                {
                    about.CvUrl = newCvUrl;
                }

                // 8. Veritabanına kaydet
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Hakkımda bilgileri kaydedilemedi.");

                // Kayıt başarısızsa yeni dosyaları temizle
                DeleteManagedFile(
                    newImageUrl, "images", "about");

                DeleteManagedFile(
                    newCvUrl, "files", "cv");

                SetExistingFiles(model, oldImageUrl, oldCvUrl);

                ModelState.AddModelError(
                    string.Empty,
                    "Hakkımda bilgileri kaydedilirken hata oluştu.");

                return View(model);
            }

            // 9. Kayıt başarılıysa eski yönetilen dosyaları temizle
            if (newImageUrl != null)
            {
                DeleteManagedFile(
                    oldImageUrl, "images", "about");
            }

            if (newCvUrl != null)
            {
                DeleteManagedFile(
                    oldCvUrl, "files", "cv");
            }

            TempData["SuccessMessage"] =
                "Hakkımda bilgileri başarıyla güncellendi.";

            return RedirectToAction(nameof(EditAbout));
        }

        // ==================== IMAGE VALIDATION ====================

        private async Task ValidateImageAsync(IFormFile? image)
        {
            var error = await UploadSafety.ValidateImageAsync(image);
            if (error != null) ModelState.AddModelError("ImageFile", error);
        }

        // ==================== CV VALIDATION ====================

        private async Task ValidateCvAsync(IFormFile? cv)
        {
            var error = await UploadSafety.ValidatePdfAsync(cv);
            if (error != null) ModelState.AddModelError("CvFile", error);
        }

        // ==================== SAVE FILE ====================

        private async Task<string> SaveFileAsync(
            IFormFile file,
            string parentFolder,
            string subFolder)
        {
            var webRoot = GetWebRoot();

            var folderPath = Path.Combine(
                webRoot,
                parentFolder,
                subFolder);

            Directory.CreateDirectory(folderPath);

            var extension = Path.GetExtension(
                file.FileName).ToLowerInvariant();

            // Kullanıcının dosya ismini kullanmıyoruz
            var fileName = $"{Guid.NewGuid():N}{extension}";

            var fullPath = Path.Combine(
                folderPath,
                fileName);

            try
            {
                await using var stream = new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write);

                if (parentFolder == "images") await UploadSafety.CopySafeImageAsync(file, stream);
                else await file.CopyToAsync(stream);
            }
            catch
            {
                try
                {
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(
                        cleanupEx,
                        "Kısmi dosya temizlenemedi: {FileName}",
                        fileName);
                }

                throw;
            }

            return $"/{parentFolder}/{subFolder}/{fileName}";
        }

        // ==================== DELETE MANAGED FILE ====================

        private void DeleteManagedFile(
            string? fileUrl,
            string parentFolder,
            string subFolder)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return;

            var prefix = $"/{parentFolder}/{subFolder}/";

            if (!fileUrl.StartsWith(
                prefix,
                StringComparison.Ordinal))
            {
                return;
            }

            var fileName = fileUrl.Substring(prefix.Length);

            // Dosya yolu yerine yalnızca dosya adı kabul et
            if (fileName != Path.GetFileName(fileName))
                return;

            var extension = Path.GetExtension(fileName)
                .ToLowerInvariant();

            var allowed = parentFolder == "images"
                ? ImageExtensions.Contains(extension)
                : extension == ".pdf";

            if (!allowed)
                return;

            // Eski manuel dosyaları korumak için yalnızca
            // uygulamanın GUID ile oluşturduğu dosyaları sil
            var name = Path.GetFileNameWithoutExtension(fileName);

            if (!Guid.TryParseExact(name, "N", out _))
                return;

            var fullPath = Path.Combine(
                GetWebRoot(),
                parentFolder,
                subFolder,
                fileName);

            try
            {
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                _logger.LogWarning(
                    ex,
                    "Eski dosya temizlenemedi: {FileName}",
                    fileName);
            }
        }

        // ==================== WEBROOT ====================

        private string GetWebRoot()
        {
            return _storage.Root;
        }

        // ==================== EXISTING FILES ====================

        private static void SetExistingFiles(
            AboutFormViewModel model,
            About? about)
        {
            model.ExistingImageUrl = about?.ImageUrl;
            model.ExistingCvUrl = about?.CvUrl;
        }

        private static void SetExistingFiles(
            AboutFormViewModel model,
            string? imageUrl,
            string? cvUrl)
        {
            model.ExistingImageUrl = imageUrl;
            model.ExistingCvUrl = cvUrl;
        }
    }
}
