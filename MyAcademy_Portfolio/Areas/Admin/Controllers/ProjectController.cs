using MyAcademy_PortfolioProject.Infrastructure;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [MyAcademy_PortfolioProject.Infrastructure.SerializedAdminWrite("Category")]
    [RequestSizeLimit(UploadSafety.RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadSafety.RequestLimit)]
    public class ProjectController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UploadStorage _storage;
        private readonly ILogger<ProjectController> _logger;



        private static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        public ProjectController(
            PortfolioContext context,
            IWebHostEnvironment environment,
            UploadStorage storage,
            ILogger<ProjectController> logger)
        {
            _context = context;
            _environment = environment;
            _storage = storage;
            _logger = logger;
        }

        // ==================== PROJECT LIST ====================

        [HttpGet]
        public async Task<IActionResult> ProjectList()
        {
            var projects = await _context.Projects
                .AsNoTracking()
                .Include(x => x.Category)
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.ProjectId)
                .ToListAsync();

            return View(projects);
        }

        // ==================== CREATE PROJECT - GET ====================

        [HttpGet]
        public async Task<IActionResult> CreateProject()
        {
            var model = new ProjectCreateViewModel
            {
                IsActive = true,
                Categories = await GetCategoriesAsync()
            };

            return View(model);
        }

        // ==================== CREATE PROJECT - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProject(
            ProjectCreateViewModel model)
        {
            await ValidateCategoryAsync(model.CategoryId);

            await ValidateImageAsync(
                model.ImageFile,
                nameof(model.ImageFile));

            if (string.IsNullOrWhiteSpace(model.ProjectTitle))
            {
                ModelState.AddModelError(
                    nameof(model.ProjectTitle),
                    "Proje adı zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Proje açıklaması zorunludur.");
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await GetCategoriesAsync();
                return View(model);
            }

            string? imageUrl = null;

            try
            {
                // 1. Görsel yükle
                if (model.ImageFile != null)
                {
                    imageUrl = await SaveImageAsync(model.ImageFile);
                }

                // 2. Entity oluştur
                var project = new Project
                {
                    ProjectTitle = model.ProjectTitle.Trim(),
                    Description = model.Description.Trim(),
                    CategoryId = model.CategoryId,
                    Technologies = model.Technologies?.Trim(),
                    GithubUrl = model.GithubUrl?.Trim(),
                    LiveUrl = model.LiveUrl?.Trim(),
                    ImageUrl = imageUrl,
                    IsActive = model.IsActive,
                    CreatedDate = DateTime.UtcNow
                };

                // 3. Veritabanına kaydet
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Proje başarıyla eklendi.";

                return RedirectToAction(nameof(ProjectList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Proje eklenemedi.");

                // Yükleme gerçekleştiyse yeni dosyayı temizle
                DeleteManagedImage(imageUrl);

                ModelState.AddModelError(
                    string.Empty,
                    "Proje kaydedilirken bir hata oluştu.");

                model.Categories = await GetCategoriesAsync();
                return View(model);
            }
        }

        // ==================== UPDATE PROJECT - GET ====================

        [HttpGet]
        public async Task<IActionResult> UpdateProject(int id)
        {
            // 1. Güncellenecek projeyi bul
            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            // 2. Entity bilgilerini ViewModel'e aktar
            var model = new ProjectUpdateViewModel
            {
                ProjectId = project.ProjectId,
                ProjectTitle = project.ProjectTitle,
                Description = project.Description,
                CategoryId = project.CategoryId,
                Technologies = project.Technologies,
                GithubUrl = project.GithubUrl,
                LiveUrl = project.LiveUrl,
                ExistingImageUrl = project.ImageUrl,
                IsActive = project.IsActive,
                Categories = await GetCategoriesAsync()
            };

            // 3. Formu mevcut bilgilerle aç
            return View(model);
        }

        // ==================== UPDATE PROJECT - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProject(
            ProjectUpdateViewModel model)
        {
            // 1. Güncellenecek projeyi veritabanından getir
            var project = await _context.Projects
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == model.ProjectId);

            if (project == null)
            {
                return NotFound();
            }

            // 2. Kategori ve görsel doğrulaması
            await ValidateCategoryAsync(model.CategoryId);

            await ValidateImageAsync(
                model.ImageFile,
                nameof(model.ImageFile));

            if (string.IsNullOrWhiteSpace(model.ProjectTitle))
            {
                ModelState.AddModelError(
                    nameof(model.ProjectTitle),
                    "Proje adı zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Proje açıklaması zorunludur.");
            }

            // 3. Model geçersizse mevcut görseli koruyarak dön
            if (!ModelState.IsValid)
            {
                model.ExistingImageUrl = project.ImageUrl;
                model.Categories = await GetCategoriesAsync();

                return View(model);
            }

            // Güncelleme öncesindeki görsel yolu
            var oldImageUrl = project.ImageUrl;

            // Yalnızca yeni yüklenen görsel
            string? newImageUrl = null;

            try
            {
                // 4. Yeni görsel seçildiyse yükle
                if (model.ImageFile != null)
                {
                    newImageUrl =
                        await SaveImageAsync(model.ImageFile);
                }

                // 5. Proje bilgilerini güncelle
                project.ProjectTitle = model.ProjectTitle.Trim();
                project.Description = model.Description.Trim();
                project.CategoryId = model.CategoryId;
                project.Technologies = model.Technologies?.Trim();
                project.GithubUrl = model.GithubUrl?.Trim();
                project.LiveUrl = model.LiveUrl?.Trim();
                project.IsActive = model.IsActive;

                // Yeni görsel yoksa eski görseli koru
                if (newImageUrl != null)
                {
                    project.ImageUrl = newImageUrl;
                }

                // CreatedDate güncellenmiyor!

                // 6. SQL Server'a kaydet
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Proje güncellenemedi. ProjectId: {ProjectId}",
                    model.ProjectId);

                // Veritabanı kaydı başarısız olduysa
                // yeni yüklenen görseli temizle
                DeleteManagedImage(newImageUrl);

                model.ExistingImageUrl = oldImageUrl;
                model.Categories = await GetCategoriesAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Proje güncellenirken bir hata oluştu.");

                return View(model);
            }

            // 7. Kayıt başarılıysa eski yönetilen görseli temizle
            if (newImageUrl != null &&
                oldImageUrl != newImageUrl)
            {
                DeleteManagedImage(oldImageUrl);
            }

            TempData["SuccessMessage"] =
                "Proje başarıyla güncellendi.";

            return RedirectToAction(nameof(ProjectList));
        }

        // ==================== DELETE PROJECT - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id)
        {
            // 1. Silinecek projeyi bul
            var project = await _context.Projects
                .FirstOrDefaultAsync(x => x.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            var oldImageUrl = project.ImageUrl;

            try
            {
                // 2. Önce veritabanındaki kaydı sil
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Proje silinemedi. ProjectId: {ProjectId}",
                    id);

                TempData["ErrorMessage"] =
                    "Proje silinirken bir hata oluştu.";

                return RedirectToAction(nameof(ProjectList));
            }

            // 3. Veritabanı kaydı silindikten sonra
            // yalnızca uygulamanın yönettiği görseli temizle
            DeleteManagedImage(oldImageUrl);

            TempData["SuccessMessage"] =
                "Proje başarıyla silindi.";

            return RedirectToAction(nameof(ProjectList));
        }

        // ==================== CATEGORY VALIDATION ====================

        private async Task ValidateCategoryAsync(int categoryId)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(x => x.CategoryId == categoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Lütfen geçerli bir kategori seçiniz.");
            }
        }

        // ==================== CATEGORY DROPDOWN ====================

        private async Task<List<SelectListItem>> GetCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .OrderBy(x => x.CategoryName)
                .Select(x => new SelectListItem
                {
                    Value = x.CategoryId.ToString(),
                    Text = x.CategoryName
                })
                .ToListAsync();
        }

        // ==================== IMAGE VALIDATION ====================

        private async Task ValidateImageAsync(IFormFile? image, string propertyName)
        {
            var error = await UploadSafety.ValidateImageAsync(image);
            if (error != null) ModelState.AddModelError(propertyName, error);
        }

        // ==================== SAVE IMAGE ====================

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var extension = Path.GetExtension(
                image.FileName).ToLowerInvariant();

            var uploadsFolder = Path.Combine(
                _storage.Root,
                "images",
                "projects");

            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";

            var filePath = Path.Combine(
                uploadsFolder,
                fileName);

            try
            {
                using var stream = new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write);

                await UploadSafety.CopySafeImageAsync(image, stream);

                return $"/images/projects/{fileName}";
            }
            catch
            {
                // Dosya kısmen yazılmış olabilir
                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Kısmi görsel dosyası temizlenemedi.");
                }

                throw;
            }
        }

        // ==================== DELETE MANAGED IMAGE ====================

        private void DeleteManagedImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return;

            const string prefix = "/images/projects/";

            if (!imageUrl.StartsWith(
                prefix,
                StringComparison.Ordinal))
            {
                return;
            }

            // Sadece uygulamamızın oluşturduğu GUID adlı
            // dosyaları otomatik silelim.
            var fileName = imageUrl.Substring(prefix.Length);

            if (fileName != Path.GetFileName(fileName))
                return;

            var extension = Path.GetExtension(fileName);

            if (!AllowedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            var nameWithoutExtension =
                Path.GetFileNameWithoutExtension(fileName);

            // "minishop.webp" gibi elle eklenen dosyaları silme
            if (nameWithoutExtension.Length != 32 ||
                !Guid.TryParseExact(
                    nameWithoutExtension,
                    "N",
                    out _))
            {
                return;
            }

            var filePath = Path.Combine(
                _storage.Root,
                "images",
                "projects",
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
                    "Proje görseli silinemedi: {FileName}",
                    fileName);
            }
        }

    }
}
