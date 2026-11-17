
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class PhilosophyFormViewModel
    {
        public int PhilosophyId { get; set; }

        [Required(ErrorMessage = "Felsefe metni zorunludur.")]
        [StringLength(2000,
            ErrorMessage = "Felsefe metni en fazla 2000 karakter olabilir.")]
        [Display(Name = "Felsefe Metni")]
        public string Quote { get; set; } = string.Empty;

        [StringLength(150,
            ErrorMessage = "Yazar adı en fazla 150 karakter olabilir.")]
        [Display(Name = "Yazar / İmza")]
        public string? Author { get; set; }

        [StringLength(1000,
            ErrorMessage = "Alt açıklama en fazla 1000 karakter olabilir.")]
        [Display(Name = "Alt Açıklama")]
        public string? Subtitle { get; set; }
    }
}
