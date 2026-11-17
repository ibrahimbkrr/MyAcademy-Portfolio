
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class SkillFormViewModel
    {
        public int SkillId { get; set; }

        [Required(ErrorMessage = "Yetenek adı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Yetenek Adı")]
        public string SkillName { get; set; } = string.Empty;

        [Range(0, 100, ErrorMessage = "Seviye 0 ile 100 arasında olmalıdır.")]
        [Display(Name = "Yetenek Seviyesi")]
        public int Level { get; set; } = 50;

        [Range(0, int.MaxValue, ErrorMessage = "Sıralama negatif olamaz.")]
        [Display(Name = "Gösterim Sırası")]
        public int DisplayOrder { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Lütfen kategori seçiniz.")]
        [Display(Name = "Yetenek Kategorisi")]
        public int SkillCategoryId { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
    }
}
