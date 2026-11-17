
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [MyAcademy_PortfolioProject.Infrastructure.SerializedAdminWrite("SkillCategory")]
    public class SkillController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ILogger<SkillController> _logger;

        public SkillController(
            PortfolioContext context,
            ILogger<SkillController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== SKILL LIST ====================

        [HttpGet]
        public async Task<IActionResult> SkillList()
        {
            var skills = await _context.Skills
                .AsNoTracking()
                .Include(x => x.SkillCategory)
                .OrderBy(x => x.SkillCategory.DisplayOrder)
                .ThenBy(x => x.DisplayOrder)
                .ThenBy(x => x.SkillName)
                .ToListAsync();

            return View(skills);
        }

        // ==================== CREATE - GET ====================

        [HttpGet]
        public async Task<IActionResult> CreateSkill()
        {
            var model = new SkillFormViewModel
            {
                Level = 50,
                Categories = await GetCategoriesAsync()
            };

            return View(model);
        }

        // ==================== CREATE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSkill(
            SkillFormViewModel model)
        {
            model.SkillName = model.SkillName?.Trim() ?? "";

            await ValidateSkillAsync(model);

            if (!ModelState.IsValid)
            {
                model.Categories = await GetCategoriesAsync();
                return View(model);
            }

            var skill = new Skill
            {
                SkillName = model.SkillName,
                Level = model.Level,
                DisplayOrder = model.DisplayOrder,
                SkillCategoryId = model.SkillCategoryId
            };

            try
            {
                _context.Skills.Add(skill);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Yetenek başarıyla eklendi.";

                return RedirectToAction(nameof(SkillList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Yetenek eklenemedi.");

                ModelState.AddModelError(
                    string.Empty,
                    "Yetenek kaydedilirken bir hata oluştu.");

                model.Categories = await GetCategoriesAsync();
                return View(model);
            }
        }

        // ==================== UPDATE - GET ====================

        [HttpGet]
        public async Task<IActionResult> UpdateSkill(int id)
        {
            var skill = await _context.Skills
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SkillId == id);

            if (skill == null)
                return NotFound();

            var model = new SkillFormViewModel
            {
                SkillId = skill.SkillId,
                SkillName = skill.SkillName,
                Level = skill.Level,
                DisplayOrder = skill.DisplayOrder,
                SkillCategoryId = skill.SkillCategoryId,
                Categories = await GetCategoriesAsync()
            };

            return View(model);
        }

        // ==================== UPDATE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSkill(
            SkillFormViewModel model)
        {
            var skill = await _context.Skills
                .FirstOrDefaultAsync(x => x.SkillId == model.SkillId);

            if (skill == null)
                return NotFound();

            model.SkillName = model.SkillName?.Trim() ?? "";

            await ValidateSkillAsync(model);

            if (!ModelState.IsValid)
            {
                model.Categories = await GetCategoriesAsync();
                return View(model);
            }

            skill.SkillName = model.SkillName;
            skill.Level = model.Level;
            skill.DisplayOrder = model.DisplayOrder;
            skill.SkillCategoryId = model.SkillCategoryId;

            try
            {
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Yetenek başarıyla güncellendi.";

                return RedirectToAction(nameof(SkillList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Yetenek güncellenemedi. Id: {Id}",
                    model.SkillId);

                ModelState.AddModelError(
                    string.Empty,
                    "Yetenek güncellenirken bir hata oluştu.");

                model.Categories = await GetCategoriesAsync();
                return View(model);
            }
        }

        // ==================== DELETE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            var skill = await _context.Skills
                .FirstOrDefaultAsync(x => x.SkillId == id);

            if (skill == null)
                return NotFound();

            try
            {
                _context.Skills.Remove(skill);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Yetenek başarıyla silindi.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Yetenek silinemedi. Id: {Id}",
                    id);

                TempData["ErrorMessage"] =
                    "Yetenek silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(SkillList));
        }

        // ==================== VALIDATION ====================

        private async Task ValidateSkillAsync(
            SkillFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.SkillName))
            {
                ModelState.AddModelError(
                    nameof(model.SkillName),
                    "Yetenek adı boş olamaz.");
            }

            var categoryExists = await _context.SkillCategories
                .AnyAsync(x =>
                    x.SkillCategoryId == model.SkillCategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(model.SkillCategoryId),
                    "Lütfen geçerli bir yetenek kategorisi seçiniz.");
            }
        }

        // ==================== CATEGORY DROPDOWN ====================

        private async Task<List<SelectListItem>> GetCategoriesAsync()
        {
            return await _context.SkillCategories
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CategoryName)
                .Select(x => new SelectListItem
                {
                    Value = x.SkillCategoryId.ToString(),
                    Text = x.CategoryName
                })
                .ToListAsync();
        }
    }
}
