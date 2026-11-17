namespace MyAcademy_PortfolioProject.Entities
{
    public class About
    {
        public int AboutId { get; set; }

        // Sol taraftaki bilgiler
        public string? ImageUrl { get; set; }
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? JobTitle { get; set; }

        // Sağ taraftaki bilgiler
        public string? SectionLabel { get; set; }
        public string? Title { get; set; }
        public string? Description1 { get; set; }
        public string? Description2 { get; set; }

        // İstatistikler
        public string? Stat1Value { get; set; }
        public string? Stat1Label { get; set; }

        public string? Stat2Value { get; set; }
        public string? Stat2Label { get; set; }

        public string? Stat3Value { get; set; }
        public string? Stat3Label { get; set; }

        // Özgeçmiş bağlantısı
        public string? CvUrl { get; set; }
    }
}