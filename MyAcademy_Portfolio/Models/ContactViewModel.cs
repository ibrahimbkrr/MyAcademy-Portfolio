using MyAcademy_PortfolioProject.Entities;


namespace MyAcademy_PortfolioProject.Models
{
    public class ContactViewModel
    {
        public Contact Contact { get; set; } = new();

        public List<SocialMedia> SocialMedias { get; set; } = new();
    }
}