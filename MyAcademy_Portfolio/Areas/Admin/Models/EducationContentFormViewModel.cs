
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class EducationContentFormViewModel
    {
        public int EducationContentId { get; set; }

        [StringLength(100)]
        [Display(Name = "Bölüm Etiketi")]
        public string? SectionLabel { get; set; }

        [Required(ErrorMessage = "Bölüm başlığı zorunludur.")]
        [StringLength(200)]
        [Display(Name = "Bölüm Başlığı")]
        public string SectionTitle { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Alıntı Metni")]
        public string? Quote { get; set; }

        [StringLength(2000)]
        [Display(Name = "Bölüm Açıklaması")]
        public string? Description { get; set; }
    }
}
