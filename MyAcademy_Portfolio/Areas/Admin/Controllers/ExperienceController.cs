
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ExperienceController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ILogger<ExperienceController> _logger;

        public ExperienceController(
            PortfolioContext context,
            ILogger<ExperienceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== EXPERIENCE LIST ====================

        [HttpGet]
        public async Task<IActionResult> ExperienceList()
        {
            var experiences = await _context.Experiences
                .AsNoTracking()
                .OrderByDescending(x => x.IsCurrent)
                .ThenByDescending(x => x.StartDate)
                .ThenByDescending(x => x.ExperienceId)
                .ToListAsync();

            return View(experiences);
        }

        // ==================== CREATE EXPERIENCE - GET ====================

        [HttpGet]
        public IActionResult CreateExperience()
        {
            var model = new ExperienceFormViewModel
            {
                IsActive = true
            };

            return View(model);
        }

        // ==================== CREATE EXPERIENCE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExperience(
            ExperienceFormViewModel model)
        {
            ValidateTextFields(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var experience = new Experience
            {
                Title = model.Title.Trim(),
                CompanyName = model.CompanyName.Trim(),
                Description = model.Description.Trim(),
                CategoryName = model.CategoryName.Trim(),
                StartDate = model.StartDate,
                EndDate = model.IsCurrent ? null : model.EndDate,
                IsCurrent = model.IsCurrent,
                IsActive = model.IsActive
            };

            try
            {
                _context.Experiences.Add(experience);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Deneyim başarıyla eklendi.";

                return RedirectToAction(nameof(ExperienceList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Deneyim kaydedilirken hata oluştu.");

                ModelState.AddModelError(
                    string.Empty,
                    "Deneyim kaydedilemedi. Lütfen tekrar deneyiniz.");

                return View(model);
            }
        }

        // ==================== UPDATE EXPERIENCE - GET ====================

        [HttpGet]
        public async Task<IActionResult> UpdateExperience(int id)
        {
            var experience = await _context.Experiences
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExperienceId == id);

            if (experience == null)
            {
                return NotFound();
            }

            var model = new ExperienceFormViewModel
            {
                ExperienceId = experience.ExperienceId,
                Title = experience.Title,
                CompanyName = experience.CompanyName,
                Description = experience.Description,
                CategoryName = experience.CategoryName,
                StartDate = experience.StartDate,
                EndDate = experience.EndDate,
                IsCurrent = experience.IsCurrent,
                IsActive = experience.IsActive
            };

            return View(model);
        }

        // ==================== UPDATE EXPERIENCE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExperience(
            ExperienceFormViewModel model)
        {
            var experience = await _context.Experiences
                .FirstOrDefaultAsync(
                    x => x.ExperienceId == model.ExperienceId);

            if (experience == null)
            {
                return NotFound();
            }

            ValidateTextFields(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            experience.Title = model.Title.Trim();
            experience.CompanyName = model.CompanyName.Trim();
            experience.Description = model.Description.Trim();
            experience.CategoryName = model.CategoryName.Trim();
            experience.StartDate = model.StartDate;
            experience.EndDate = model.IsCurrent ? null : model.EndDate;
            experience.IsCurrent = model.IsCurrent;
            experience.IsActive = model.IsActive;

            try
            {
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Deneyim başarıyla güncellendi.";

                return RedirectToAction(nameof(ExperienceList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Deneyim güncellenirken hata oluştu. Id: {Id}",
                    model.ExperienceId);

                ModelState.AddModelError(
                    string.Empty,
                    "Deneyim güncellenemedi. Lütfen tekrar deneyiniz.");

                return View(model);
            }
        }

        // ==================== DELETE EXPERIENCE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExperience(int id)
        {
            var experience = await _context.Experiences
                .FirstOrDefaultAsync(x => x.ExperienceId == id);

            if (experience == null)
            {
                return NotFound();
            }

            try
            {
                _context.Experiences.Remove(experience);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Deneyim başarıyla silindi.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Deneyim silinirken hata oluştu. Id: {Id}",
                    id);

                TempData["ErrorMessage"] =
                    "Deneyim silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(ExperienceList));
        }

        // ==================== TEXT VALIDATION ====================

        private void ValidateTextFields(
            ExperienceFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Deneyim başlığı boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.CompanyName))
            {
                ModelState.AddModelError(
                    nameof(model.CompanyName),
                    "Şirket veya kurum adı boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Açıklama boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "Deneyim türü boş olamaz.");
            }
        }
    }
}
