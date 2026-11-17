
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class EducationController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ILogger<EducationController> _logger;

        public EducationController(
            PortfolioContext context,
            ILogger<EducationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== EDUCATION LIST ====================

        [HttpGet]
        public async Task<IActionResult> EducationList()
        {
            var educations = await _context.Educations
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.StartDate)
                .ThenByDescending(x => x.EducationId)
                .ToListAsync();

            return View(educations);
        }

        // ==================== CREATE - GET ====================

        [HttpGet]
        public IActionResult CreateEducation()
        {
            var model = new EducationFormViewModel
            {
                IsActive = true,
                DisplayOrder = 0
            };

            return View(model);
        }

        // ==================== CREATE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEducation(
            EducationFormViewModel model)
        {
            ValidateTextFields(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var education = new Education
            {
                Title = model.Title.Trim(),
                Institution = model.Institution.Trim(),
                Description = model.Description?.Trim(),
                EducationType = model.EducationType.Trim(),
                StartDate = model.StartDate,
                EndDate = model.IsCurrent ? null : model.EndDate,
                IsCurrent = model.IsCurrent,
                DisplayOrder = model.DisplayOrder,
                IsActive = model.IsActive
            };

            try
            {
                _context.Educations.Add(education);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Eğitim başarıyla eklendi.";

                return RedirectToAction(nameof(EducationList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Eğitim kaydedilemedi.");

                ModelState.AddModelError(
                    string.Empty,
                    "Eğitim kaydedilirken bir hata oluştu.");

                return View(model);
            }
        }

        // ==================== UPDATE - GET ====================

        [HttpGet]
        public async Task<IActionResult> UpdateEducation(int id)
        {
            var education = await _context.Educations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EducationId == id);

            if (education == null)
            {
                return NotFound();
            }

            var model = new EducationFormViewModel
            {
                EducationId = education.EducationId,
                Title = education.Title ?? string.Empty,
                Institution = education.Institution ?? string.Empty,
                Description = education.Description,
                EducationType = education.EducationType ?? string.Empty,
                StartDate = education.StartDate,
                EndDate = education.EndDate,
                IsCurrent = education.IsCurrent,
                DisplayOrder = education.DisplayOrder,
                IsActive = education.IsActive
            };

            return View(model);
        }

        // ==================== UPDATE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEducation(
            EducationFormViewModel model)
        {
            var education = await _context.Educations
                .FirstOrDefaultAsync(
                    x => x.EducationId == model.EducationId);

            if (education == null)
            {
                return NotFound();
            }

            ValidateTextFields(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            education.Title = model.Title.Trim();
            education.Institution = model.Institution.Trim();
            education.Description = model.Description?.Trim();
            education.EducationType = model.EducationType.Trim();
            education.StartDate = model.StartDate;
            education.EndDate = model.IsCurrent ? null : model.EndDate;
            education.IsCurrent = model.IsCurrent;
            education.DisplayOrder = model.DisplayOrder;
            education.IsActive = model.IsActive;

            try
            {
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Eğitim başarıyla güncellendi.";

                return RedirectToAction(nameof(EducationList));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Eğitim güncellenemedi. Id: {Id}",
                    model.EducationId);

                ModelState.AddModelError(
                    string.Empty,
                    "Eğitim güncellenirken bir hata oluştu.");

                return View(model);
            }
        }

        // ==================== DELETE - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEducation(int id)
        {
            var education = await _context.Educations
                .FirstOrDefaultAsync(x => x.EducationId == id);

            if (education == null)
            {
                return NotFound();
            }

            try
            {
                _context.Educations.Remove(education);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Eğitim başarıyla silindi.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Eğitim silinemedi. Id: {Id}",
                    id);

                TempData["ErrorMessage"] =
                    "Eğitim silinirken bir hata oluştu.";
            }

            return RedirectToAction(nameof(EducationList));
        }

        // ==================== VALIDATION ====================

        private void ValidateTextFields(
            EducationFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Eğitim başlığı boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.Institution))
            {
                ModelState.AddModelError(
                    nameof(model.Institution),
                    "Kurum adı boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.EducationType))
            {
                ModelState.AddModelError(
                    nameof(model.EducationType),
                    "Eğitim türü boş olamaz.");
            }
        }
    }
}
