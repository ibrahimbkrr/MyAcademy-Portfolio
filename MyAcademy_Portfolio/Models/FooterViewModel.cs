using MyAcademy_PortfolioProject.Entities;


namespace MyAcademy_PortfolioProject.Models
{
    public class FooterViewModel
    {
        public Footer Footer { get; set; } = new();

        public List<SocialMedia> SocialMedias { get; set; } = new();
    }
}