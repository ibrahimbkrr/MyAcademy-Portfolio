using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public int TotalMessages { get; set; }

        public int UnreadMessages { get; set; }

        public int ReadMessages { get; set; }

        public int ReadRate =>
            TotalMessages == 0
                ? 0
                : (int)Math.Round(
                    ReadMessages * 100.0 / TotalMessages);

        public List<DateTime> WeeklyDates { get; set; } = new();

        public List<int> WeeklyCounts { get; set; } = new();

        public List<Message> RecentMessages { get; set; } = new();
    }
}