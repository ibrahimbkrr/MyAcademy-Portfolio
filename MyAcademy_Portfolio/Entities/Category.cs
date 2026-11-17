using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public string? FilterKey { get; set; }

        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}