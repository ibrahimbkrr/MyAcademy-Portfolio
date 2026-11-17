
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class ContactFormViewModel
    {
        public int ContactId { get; set; }

        [Required(ErrorMessage = "Bölüm etiketi zorunludur.")]
        [StringLength(100, ErrorMessage = "En fazla 100 karakter olabilir.")]
        [Display(Name = "Bölüm Etiketi")]
        public string SectionLabel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bölüm başlığı zorunludur.")]
        [StringLength(200, ErrorMessage = "En fazla 200 karakter olabilir.")]
        [Display(Name = "Bölüm Başlığı")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(254)]
        [Display(Name = "E-posta Adresi")]
        public string Email { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Konum en fazla 150 karakter olabilir.")]
        [Display(Name = "Konum")]
        public string? Location { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [StringLength(30, ErrorMessage = "Telefon numarası en fazla 30 karakter olabilir.")]
        [Display(Name = "Telefon Numarası")]
        public string? PhoneNumber { get; set; }
    }
}
