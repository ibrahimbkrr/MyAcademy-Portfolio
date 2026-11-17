
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [MyAcademy_PortfolioProject.Infrastructure.SerializedAdminWrite("SkillCategory")]
    public class SkillCategoryController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ILogger<SkillCategoryController> _logger;

        public SkillCategoryController(
            PortfolioContext context,
            ILogger<SkillCategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== CATEGORY LIST ====================

        [HttpGet]
        public async Task<IActionResult> CategoryList()
        {
            var categories = await _context.SkillCategories
                .AsNoTracking()
                .Include(x => x.Skills)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CategoryName)
                .ToListAsync();

            return View(categories);
        }

        // ==================== CREATE - GET ====================

        [HttpGet]
        public IActionResult CreateCategory()
        {
            return View(new SkillCategoryFormViewModel());
        }

        // ==================== CREATE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(
            SkillCategoryFormViewModel model)
        {
            NormalizeModel(model);

            await ValidateCategoryNameAsync(model.CategoryName);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var category = new SkillCategory
            {
                CategoryName = model.CategoryName,
                Description = model.Description,
                IconClass = model.IconClass,
                DisplayOrder = model.DisplayOrder
            };

            try
            {
                _context.SkillCategories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Yetenek kategorisi başarıyla eklendi.";

                return RedirectToAction(nameof(CategoryList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Yetenek kategorisi eklenemedi.");

                ModelState.AddModelError(
                    string.Empty,
                    "Kategori kaydedilirken bir hata oluştu.");

                return View(model);
            }
        }

        // ==================== UPDATE - GET ====================

        [HttpGet]
        public async Task<IActionResult> UpdateCategory(int id)
        {
            var category = await _context.SkillCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SkillCategoryId == id);

            if (category == null)
                return NotFound();

            var model = new SkillCategoryFormViewModel
            {
                SkillCategoryId = category.SkillCategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                IconClass = category.IconClass,
                DisplayOrder = category.DisplayOrder
            };

            return View(model);
        }

        // ==================== UPDATE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory(
            SkillCategoryFormViewModel model)
        {
            var category = await _context.SkillCategories
                .FirstOrDefaultAsync(
                    x => x.SkillCategoryId == model.SkillCategoryId);

            if (category == null)
                return NotFound();

            NormalizeModel(model);

            await ValidateCategoryNameAsync(
                model.CategoryName,
                model.SkillCategoryId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            category.CategoryName = model.CategoryName;
            category.Description = model.Description;
            category.IconClass = model.IconClass;
            category.DisplayOrder = model.DisplayOrder;

            try
            {
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Yetenek kategorisi başarıyla güncellendi.";

                return RedirectToAction(nameof(CategoryList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Yetenek kategorisi güncellenemedi. Id: {Id}",
                    model.SkillCategoryId);

                ModelState.AddModelError(
                    string.Empty,
                    "Kategori güncellenirken bir hata oluştu.");

                return View(model);
            }
        }

        // ==================== DELETE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.SkillCategories
                .FirstOrDefaultAsync(x => x.SkillCategoryId == id);

            if (category == null)
                return NotFound();

            // Bu kategoriye bağlı yetenek var mı?
            var hasSkills = await _context.Skills
                .AnyAsync(x => x.SkillCategoryId == id);

            if (hasSkills)
            {
                TempData["ErrorMessage"] =
                    "Bu kategoriye bağlı yetenekler bulunduğu için silinemez.";

                return RedirectToAction(nameof(CategoryList));
            }

            try
            {
                _context.SkillCategories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Yetenek kategorisi başarıyla silindi.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Yetenek kategorisi silinemedi. Id: {Id}",
                    id);

                TempData["ErrorMessage"] =
                    "Kategori silinemedi. Bağlı kayıtları kontrol ediniz.";
            }

            return RedirectToAction(nameof(CategoryList));
        }

        // ==================== HELPERS ====================

        private static void NormalizeModel(
            SkillCategoryFormViewModel model)
        {
            model.CategoryName = model.CategoryName?.Trim() ?? "";
            model.Description = model.Description?.Trim();
            model.IconClass = model.IconClass?.Trim();
        }

        private async Task ValidateCategoryNameAsync(
            string categoryName,
            int? excludedId = null)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Kategori adı boş olamaz.");

                return;
            }

            var nameExists = await _context.SkillCategories
                .AnyAsync(x =>
                    x.CategoryName == categoryName &&
                    (!excludedId.HasValue ||
                     x.SkillCategoryId != excludedId.Value));

            if (nameExists)
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Bu kategori adı zaten kullanılıyor.");
            }
        }
    }
}
