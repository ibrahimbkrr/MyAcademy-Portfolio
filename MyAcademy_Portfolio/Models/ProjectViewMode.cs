using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Models
{
    public class ProjectViewModel
    {
        public string? AllProjectsUrl { get; set; }
        public List<Project> Projects { get; set; } = new();

        public List<Category> Categories { get; set; } = new();
    }
}
