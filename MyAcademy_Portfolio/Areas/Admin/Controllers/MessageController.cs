
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MessageController : Controller
    {
        private readonly PortfolioContext _context;

        public MessageController(PortfolioContext context)
        {
            _context = context;
        }

        // LIST
        [HttpGet]
        public async Task<IActionResult> MessageList()
        {
            var messages = await _context.Messages
                .AsNoTracking()
                .OrderBy(x => x.IsRead)
                .ThenByDescending(x => x.SentDate)
                .ToListAsync();

            return View(messages);
        }

        // CREATE - GET
        [HttpGet]
        public IActionResult CreateMessage()
        {
            return View(new Message());
        }

        // CREATE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMessage(
            [Bind("SenderName,SenderEmail,Subject,MessageContent")]
            Message model)
        {
            ModelState.Remove(nameof(Message.SentDate));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var message = new Message
            {
                SenderName = model.SenderName.Trim(),
                SenderEmail = model.SenderEmail.Trim(),
                Subject = model.Subject?.Trim(),
                MessageContent = model.MessageContent.Trim(),
                SentDate = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Mesaj başarıyla oluşturuldu.";

            return RedirectToAction(nameof(MessageList));
        }

        // DETAILS - GET
        [HttpGet]
        public async Task<IActionResult> MessageDetails(int id)
        {
            var message = await _context.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MessageId == id);

            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        // MARK AS READ - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var message = await _context.Messages.FindAsync(id);

            if (message == null)
            {
                return NotFound();
            }

            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] =
                "Mesaj okundu olarak işaretlendi.";

            return RedirectToAction(nameof(MessageDetails),
                new { id });
        }

        // MARK AS UNREAD - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUnread(int id)
        {
            var message = await _context.Messages.FindAsync(id);

            if (message == null)
            {
                return NotFound();
            }

            if (message.IsRead)
            {
                message.IsRead = false;
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] =
                "Mesaj okunmadı olarak işaretlendi.";

            return RedirectToAction(nameof(MessageDetails),
                new { id });
        }

        // UPDATE - GET
        [HttpGet]
        public async Task<IActionResult> UpdateMessage(int id)
        {
            var message = await _context.Messages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MessageId == id);

            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        // UPDATE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMessage(
            [Bind("MessageId,SenderName,SenderEmail,Subject,MessageContent,IsRead")]
            Message model)
        {
            var message = await _context.Messages
                .FindAsync(model.MessageId);

            if (message == null)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(Message.SentDate));

            if (!ModelState.IsValid)
            {
                model.SentDate = message.SentDate;
                return View(model);
            }

            message.SenderName = model.SenderName.Trim();
            message.SenderEmail = model.SenderEmail.Trim();
            message.Subject = model.Subject?.Trim();
            message.MessageContent = model.MessageContent.Trim();
            message.IsRead = model.IsRead;

            // SentDate değiştirilmez.
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Mesaj başarıyla güncellendi.";

            return RedirectToAction(nameof(MessageList));
        }

        // DELETE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.Messages.FindAsync(id);

            if (message == null)
            {
                return NotFound();
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Mesaj başarıyla silindi.";

            return RedirectToAction(nameof(MessageList));
        }
    }
}
