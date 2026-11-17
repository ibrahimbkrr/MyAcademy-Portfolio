
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class SocialMediaFormViewModel
    {
        public int SocialMediaId { get; set; }

        [Required(ErrorMessage = "Platform adı zorunludur.")]
        [StringLength(50, ErrorMessage = "Platform adı en fazla 50 karakter olabilir.")]
        [Display(Name = "Platform Adı")]
        public string Platform { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bağlantı zorunludur.")]
        [Url(ErrorMessage = "Geçerli bir URL giriniz.")]
        [StringLength(2048, ErrorMessage = "Bağlantı çok uzundur.")]
        [Display(Name = "Profil Bağlantısı")]
        public string Url { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "İkon Sınıfı")]
        public string? IconClass { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Sıralama negatif olamaz.")]
        [Display(Name = "Gösterim Sırası")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Aktif Mi?")]
        public bool IsActive { get; set; } = true;
    }
}
