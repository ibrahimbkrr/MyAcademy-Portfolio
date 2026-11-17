using MyAcademy_PortfolioProject.Entities;


namespace MyAcademy_PortfolioProject.Models
{
    public class HeroSectionViewModel
    {
        public string? Greeting { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? MainImageUrl { get; set; }

        public string? BottomBadgeValue { get; set; }
        public string? BottomBadgeText { get; set; }

        public string? TopBadgeValue { get; set; }
        public string? TopBadgeText { get; set; }

        public List<string> HeroTitles { get; set; } = new();

        public List<SocialMedia> SocialMedias { get; set; } = new();
    }
}