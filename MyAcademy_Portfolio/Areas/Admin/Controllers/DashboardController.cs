
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;
using MyAcademy_PortfolioProject.Areas.Admin.Models;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly PortfolioContext _context;

        public DashboardController(PortfolioContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            // 1. Toplam mesaj sayısı
            model.TotalMessages = await _context.Messages
                .AsNoTracking()
                .CountAsync();

            // 2. Okunmamış mesaj sayısı
            model.UnreadMessages = await _context.Messages
                .AsNoTracking()
                .CountAsync(x => !x.IsRead);

            // 3. Okunmuş mesaj sayısı
            model.ReadMessages =
                model.TotalMessages - model.UnreadMessages;

            // 4. Son 7 günün tarihlerini hazırla
            // Mesaj tarihleri UTC olarak kaydediliyor.
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-6);
            var endDate = today.AddDays(1);

            model.WeeklyDates = Enumerable
                .Range(0, 7)
                .Select(i => startDate.AddDays(i))
                .ToList();

            // 5. Günlük mesaj sayılarını veritabanından getir
            var dailyMessages = await _context.Messages
                .AsNoTracking()
                .Where(x => x.SentDate >= startDate &&
                            x.SentDate < endDate)
                .GroupBy(x => x.SentDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Mesaj olmayan günler için 0 değeri ata
            model.WeeklyCounts = model.WeeklyDates
                .Select(date => dailyMessages
                    .FirstOrDefault(x => x.Date == date)?.Count ?? 0)
                .ToList();

            // 6. En son gelen 5 mesajı getir
            model.RecentMessages = await _context.Messages
                .AsNoTracking()
                .OrderByDescending(x => x.SentDate)
                .ThenByDescending(x => x.MessageId)
                .Take(5)
                .ToListAsync();

            return View(model);
        }
    }
}
