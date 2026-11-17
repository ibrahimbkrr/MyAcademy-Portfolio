using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class CategoryUpdateViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Filtre anahtarı zorunludur.")]
        [RegularExpression(
            @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
            ErrorMessage = "Yalnızca küçük harf, rakam ve kelimeler arasında tire kullanabilirsiniz.")]
        [StringLength(50)]
        public string FilterKey { get; set; } = string.Empty;
    }
}