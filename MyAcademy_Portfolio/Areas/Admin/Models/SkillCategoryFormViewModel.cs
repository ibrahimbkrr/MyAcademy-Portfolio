
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class SkillCategoryFormViewModel
    {
        public int SkillCategoryId { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(100, ErrorMessage = "En fazla 100 karakter girebilirsiniz.")]
        [Display(Name = "Kategori Adı")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [StringLength(100)]
        [Display(Name = "İkon Sınıfı")]
        public string? IconClass { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Sıralama negatif olamaz.")]
        [Display(Name = "Gösterim Sırası")]
        public int DisplayOrder { get; set; }
    }
}
