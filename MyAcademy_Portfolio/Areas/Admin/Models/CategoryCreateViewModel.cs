using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir.")]
        [Display(Name = "Kategori Adı")]
        public string CategoryName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Filtre anahtarı zorunludur.")]
        [RegularExpression(
            @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            ErrorMessage = "Yalnızca küçük harf, rakam ve kelimeler arasında tire kullanabilirsiniz.")]
        [StringLength(50)]
        [Display(Name = "Filtre Anahtarı")]
        public string FilterKey { get; set; } = string.Empty;
    }
}