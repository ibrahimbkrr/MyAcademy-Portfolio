using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Entities
{
    public class SkillCategory
    {
        public int SkillCategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? IconClass { get; set; }

        public int DisplayOrder { get; set; }

        public List<Skill> Skills { get; set; } = new();
    }
}