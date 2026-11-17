using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Models
{
    public class ContactMessageInputModel
    {
        [Required(ErrorMessage = "Ad soyad alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Konu en fazla 200 karakter olabilir.")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Mesaj alanı zorunludur.")]
        [StringLength(5000, ErrorMessage = "Mesaj en fazla 5000 karakter olabilir.")]
        public string Content { get; set; } = string.Empty;
    }
}