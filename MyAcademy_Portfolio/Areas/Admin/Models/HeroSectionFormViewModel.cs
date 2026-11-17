
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class HeroSectionFormViewModel
    {
        public int HeroSectionId { get; set; }

        [Required(ErrorMessage = "Karşılama metni zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Karşılama Metni")]
        public string Greeting { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ana başlık zorunludur.")]
        [StringLength(200)]
        [Display(Name = "Ana Başlık")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [StringLength(2000)]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;

        // Görsel yönetimi
        public string? ExistingImageUrl { get; set; }

        [Display(Name = "Hero Görseli")]
        public IFormFile? ImageFile { get; set; }

        // Üst rozet
        [StringLength(100)]
        [Display(Name = "Üst Rozet Değeri")]
        public string? TopBadgeValue { get; set; }

        [StringLength(150)]
        [Display(Name = "Üst Rozet Açıklaması")]
        public string? TopBadgeText { get; set; }

        // Alt rozet
        [StringLength(100)]
        [Display(Name = "Alt Rozet Değeri")]
        public string? BottomBadgeValue { get; set; }

        [StringLength(150)]
        [Display(Name = "Alt Rozet Açıklaması")]
        public string? BottomBadgeText { get; set; }
    }
}
