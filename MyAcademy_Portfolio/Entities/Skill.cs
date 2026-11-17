using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Entities
{
    public class Skill
    {
        public int SkillId { get; set; }

        [Required(ErrorMessage = "Yetenek adı zorunludur.")]
        [StringLength(100)]
        public string SkillName { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Seviye 0 ile 100 arasında olmalıdır.")]
        public int Level { get; set; }

        public int DisplayOrder { get; set; }

        public int SkillCategoryId { get; set; }

        public SkillCategory SkillCategory { get; set; } = null!;
    }
}