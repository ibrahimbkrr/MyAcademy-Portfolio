using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Models
{
    public class EducationViewModel
    {
        public List<Education> Educations { get; set; } = new();

        public EducationContent EducationContent { get; set; } = new();
    }
}