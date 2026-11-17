namespace MyAcademy_PortfolioProject.Entities
{
    public class HeroSection
    {
        public int HeroSectionId { get; set; }
        public string? Greeting { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? MainImageUrl { get; set; }

        public string? BottomBadgeValue { get; set; }
        public string? BottomBadgeText { get; set; }

        public string? TopBadgeValue { get; set; }
        public string? TopBadgeText { get; set; }
    }
}
