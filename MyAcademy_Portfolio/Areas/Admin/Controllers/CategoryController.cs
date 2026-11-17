
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [MyAcademy_PortfolioProject.Infrastructure.SerializedAdminWrite("Category")]
    public class CategoryController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            PortfolioContext context,
            ILogger<CategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== CATEGORY LIST ====================

        [HttpGet]
        public async Task<IActionResult> CategoryList()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Include(x => x.Projects)
                .OrderBy(x => x.CategoryName)
                .ToListAsync();

            return View(categories);
        }

        // ==================== CREATE CATEGORY - GET ====================

        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View(new CategoryCreateViewModel());
        }

        // ==================== CREATE CATEGORY - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(
            CategoryCreateViewModel model)
        {
            // 1. Alanlardaki gereksiz boşlukları temizle
            model.CategoryName = model.CategoryName?.Trim() ?? "";
            model.FilterKey = model.FilterKey?.Trim() ?? "";

            // 2. Boşluk içeren alanları kontrol et
            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "Kategori adı zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(model.FilterKey))
            {
                ModelState.AddModelError(
                    nameof(model.FilterKey),
                    "Filtre anahtarı zorunludur.");
            }

            // 3. Aynı kategori adı daha önce kullanılmış mı?
            if (!string.IsNullOrWhiteSpace(model.CategoryName))
            {
                var nameExists = await _context.Categories
                    .AnyAsync(x =>
                        x.CategoryName == model.CategoryName);

                if (nameExists)
                {
                    ModelState.AddModelError(
                        nameof(model.CategoryName),
                        "Bu kategori adı zaten kullanılıyor.");
                }
            }

            // 4. FilterKey benzersiz olmalı
            if (!string.IsNullOrWhiteSpace(model.FilterKey))
            {
                var filterExists = await _context.Categories
                    .AnyAsync(x =>
                        x.FilterKey == model.FilterKey);

                if (filterExists)
                {
                    ModelState.AddModelError(
                        nameof(model.FilterKey),
                        "Bu filtre anahtarı zaten kullanılıyor.");
                }
            }

            // 5. Form geçerli değilse geri dön
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 6. Entity oluştur
            var category = new Category
            {
                CategoryName = model.CategoryName,
                FilterKey = model.FilterKey
            };

            // 7. Veritabanına kaydet
            try
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Kategori başarıyla eklendi.";

                return RedirectToAction(nameof(CategoryList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Kategori eklenemedi.");

                ModelState.AddModelError(
                    string.Empty,
                    "Kategori kaydedilirken bir hata oluştu.");

                return View(model);
            }
        }

        // ==================== UPDATE CATEGORY - GET ====================

        [HttpGet]
        public async Task<IActionResult> UpdateCategory(int id)
        {
            // 1. Kategoriyi getir
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            // 2. Entity -> ViewModel
            var model = new CategoryUpdateViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                FilterKey = category.FilterKey ?? ""
            };

            return View(model);
        }

        // ==================== UPDATE CATEGORY - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(
            CategoryUpdateViewModel model)
        {
            // 1. Güncellenecek kategoriyi bul
            var category = await _context.Categories
                .FirstOrDefaultAsync(x =>
                    x.CategoryId == model.CategoryId);

            if (category == null)
            {
                return NotFound();
            }

            // 2. Metinleri temizle
            model.CategoryName = model.CategoryName?.Trim() ?? "";
            model.FilterKey = model.FilterKey?.Trim() ?? "";

            // 3. Boşluk kontrolü
            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "Kategori adı zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(model.FilterKey))
            {
                ModelState.AddModelError(
                    nameof(model.FilterKey),
                    "Filtre anahtarı zorunludur.");
            }

            // 4. Aynı adı başka kategori kullanıyor mu?
            if (!string.IsNullOrWhiteSpace(model.CategoryName))
            {
                var nameExists = await _context.Categories
                    .AnyAsync(x =>
                        x.CategoryName == model.CategoryName &&
                        x.CategoryId != model.CategoryId);

                if (nameExists)
                {
                    ModelState.AddModelError(
                        nameof(model.CategoryName),
                        "Bu kategori adı başka bir kategoride kullanılıyor.");
                }
            }

            // 5. FilterKey başka kategoride var mı?
            if (!string.IsNullOrWhiteSpace(model.FilterKey))
            {
                var filterExists = await _context.Categories
                    .AnyAsync(x =>
                        x.FilterKey == model.FilterKey &&
                        x.CategoryId != model.CategoryId);

                if (filterExists)
                {
                    ModelState.AddModelError(
                        nameof(model.FilterKey),
                        "Bu filtre anahtarı başka bir kategoride kullanılıyor.");
                }
            }

            // 6. Model geçerli değilse geri dön
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 7. Verileri güncelle
            category.CategoryName = model.CategoryName;
            category.FilterKey = model.FilterKey;

            try
            {
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Kategori başarıyla güncellendi.";

                return RedirectToAction(nameof(CategoryList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Kategori güncellenemedi. CategoryId: {CategoryId}",
                    model.CategoryId);

                ModelState.AddModelError(
                    string.Empty,
                    "Kategori güncellenirken bir hata oluştu.");

                return View(model);
            }
        }

        // ==================== DELETE CATEGORY - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            // 1. Silinecek kategoriyi bul
            var category = await _context.Categories
                .FirstOrDefaultAsync(x => x.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            // 2. Bu kategoriye bağlı proje var mı?
            var hasProjects = await _context.Projects
                .AnyAsync(x => x.CategoryId == id);

            if (hasProjects)
            {
                TempData["ErrorMessage"] =
                    "Bu kategoriye bağlı projeler bulunduğu için silinemez.";

                return RedirectToAction(nameof(CategoryList));
            }

            // 3. Kategoriyi sil
            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Kategori başarıyla silindi.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Kategori silinemedi. CategoryId: {CategoryId}",
                    id);

                TempData["ErrorMessage"] =
                    "Kategori silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(CategoryList));
        }
    }
}
