using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Entities
{
    public class Project
    {
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Proje adı zorunludur.")]
        [StringLength(150)]
        public string ProjectTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public string? Technologies { get; set; }

        public string? GithubUrl { get; set; }

        public string? LiveUrl { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;
    }
}