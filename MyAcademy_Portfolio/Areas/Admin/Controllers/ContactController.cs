
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [MyAcademy_PortfolioProject.Infrastructure.SerializedAdminWrite("Contact")]
    public class ContactController : Controller
    {
        private readonly PortfolioContext _context;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            PortfolioContext context,
            ILogger<ContactController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== CONTACT LIST REDIRECT ====================

        [HttpGet]
        public IActionResult ContactList()
        {
            return RedirectToAction(nameof(EditContact));
        }

        // ==================== EDIT CONTACT - GET ====================

        [HttpGet]
        public async Task<IActionResult> EditContact()
        {
            var contact = await _context.Contacts
                .AsNoTracking()
                .OrderBy(x => x.ContactId)
                .FirstOrDefaultAsync();

            var model = new ContactFormViewModel();

            if (contact != null)
            {
                model.ContactId = contact.ContactId;
                model.SectionLabel = contact.SectionLabel;
                model.Title = contact.Title;
                model.Description = contact.Description;
                model.Email = contact.Email;
                model.Location = contact.Location;
                model.PhoneNumber = contact.PhoneNumber;
            }

            return View(model);
        }

        // ==================== EDIT CONTACT - POST ====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditContact(
            ContactFormViewModel model)
        {
            // Zorunlu alanlar yalnızca boşluk içeremez
            if (string.IsNullOrWhiteSpace(model.SectionLabel))
            {
                ModelState.AddModelError(
                    nameof(model.SectionLabel),
                    "Bölüm etiketi boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Bölüm başlığı boş olamaz.");
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "E-posta adresi boş olamaz.");
            }

            // Geçersiz bilgiler veritabanına gönderilmez
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Mevcut Contact kaydını getir
                var contact = await _context.Contacts
                    .OrderBy(x => x.ContactId)
                    .FirstOrDefaultAsync();

                // Henüz kayıt yoksa ilk kaydı oluştur
                if (contact == null)
                {
                    contact = new Contact();

                    _context.Contacts.Add(contact);
                }

                // Form bilgilerini aktar
                contact.SectionLabel =
                    model.SectionLabel.Trim();

                contact.Title =
                    model.Title.Trim();

                contact.Description =
                    model.Description?.Trim();

                contact.Email =
                    model.Email.Trim();

                contact.Location =
                    model.Location?.Trim();

                contact.PhoneNumber =
                    model.PhoneNumber?.Trim();

                // SQL Server'a kaydet
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "İletişim bilgileri başarıyla güncellendi.";

                return RedirectToAction(nameof(EditContact));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "İletişim bilgileri kaydedilemedi.");

                ModelState.AddModelError(
                    string.Empty,
                    "İletişim bilgileri kaydedilirken bir hata oluştu.");

                return View(model);
            }
        }
    }
}
