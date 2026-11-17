
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class HeroTitleFormViewModel
    {
        public int HeroTitleId { get; set; }

        [Required(ErrorMessage = "Hareketli başlık zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Hareketli Başlık")]
        public string Title { get; set; } = string.Empty;

        [Range(0, int.MaxValue,
            ErrorMessage = "Sıralama negatif olamaz.")]
        [Display(Name = "Gösterim Sırası")]
        public int Order { get; set; }

        [Display(Name = "Aktif Mi?")]
        public bool IsActive { get; set; } = true;
    }
}
