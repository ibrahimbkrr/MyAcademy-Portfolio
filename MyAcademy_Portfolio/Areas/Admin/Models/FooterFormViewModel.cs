
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class FooterFormViewModel
    {
        public int FooterId { get; set; }

        [Required(ErrorMessage = "Marka veya isim alanı zorunludur.")]
        [StringLength(100,
            ErrorMessage = "Marka adı en fazla 100 karakter olabilir.")]
        [Display(Name = "Marka / İsim")]
        public string BrandName { get; set; } = string.Empty;

        [StringLength(250,
            ErrorMessage = "Açıklama en fazla 250 karakter olabilir.")]
        [Display(Name = "Footer Açıklaması")]
        public string? Description { get; set; }

        [StringLength(250,
            ErrorMessage = "Telif hakkı metni en fazla 250 karakter olabilir.")]
        [Display(Name = "Telif Hakkı Metni")]
        public string? CopyrightText { get; set; }
    }
}
