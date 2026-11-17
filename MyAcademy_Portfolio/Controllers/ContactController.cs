
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyAcademy_PortfolioProject.Infrastructure;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;
using MyAcademy_PortfolioProject.Models;

namespace MyAcademy_PortfolioProject.Controllers
{
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(HostingSecurity.ContactPolicy)]
        [RequestSizeLimit(32 * 1024)]
        public async Task<IActionResult> Send(ContactMessageInputModel input)
        {
            // 1. Boşluk içeren zorunlu alanları kontrol et
            if (string.IsNullOrWhiteSpace(input.FullName))
            {
                ModelState.AddModelError(
                    nameof(input.FullName),
                    "Ad soyad alanı zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(input.Content))
            {
                ModelState.AddModelError(
                    nameof(input.Content),
                    "Mesaj alanı zorunludur.");
            }

            // 2. Form doğrulaması
            if (!ModelState.IsValid)
            {
                TempData["ContactError"] =
                    "Lütfen form bilgilerini kontrol ederek tekrar deneyiniz.";

                return Redirect("/#contact");
            }

            // 3. Gelen form verilerini Message Entity'sine aktar
            var message = new Message
            {
                SenderName = input.FullName.Trim(),
                SenderEmail = input.Email.Trim(),
                Subject = input.Subject?.Trim(),
                MessageContent = input.Content.Trim(),
                SentDate = DateTime.UtcNow,
                IsRead = false
            };

            // 4. Mesajı veritabanına kaydet
            try
            {
                _context.Messages.Add(message);

                await _context.SaveChangesAsync();

                TempData["ContactSuccess"] =
                    "Mesajınız başarıyla gönderildi. En kısa sürede dönüş yapacağım.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "İletişim formundan gelen mesaj kaydedilemedi.");

                TempData["ContactError"] =
                    "Mesaj gönderilirken bir hata oluştu. Lütfen tekrar deneyiniz.";
            }

            // 5. Kullanıcıyı iletişim bölümüne geri yönlendir
            return RedirectToAction("Index", "Default", fragment: "contact");
        }
    }
}
