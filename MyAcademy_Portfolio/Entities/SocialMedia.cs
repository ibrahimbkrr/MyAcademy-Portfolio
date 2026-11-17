using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Entities
{
    public class SocialMedia
    {
        public int SocialMediaId { get; set; }

        [Required(ErrorMessage = "Platform adı zorunludur.")]
        [StringLength(50)]
        public string Platform { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bağlantı zorunludur.")]
        [Url(ErrorMessage = "Geçerli bir bağlantı giriniz.")]
        public string Url { get; set; } = string.Empty;

        [StringLength(50)]
        public string? IconClass { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}