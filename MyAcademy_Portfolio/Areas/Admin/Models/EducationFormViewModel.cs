
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class EducationFormViewModel : IValidatableObject
    {
        public int EducationId { get; set; }

        [Required(ErrorMessage = "Eğitim başlığı zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Eğitim Başlığı")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kurum adı zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Eğitim Kurumu")]
        public string Institution { get; set; } = string.Empty;

        [StringLength(2000)]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Eğitim türü zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Eğitim Türü")]
        public string EducationType { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Başlangıç Tarihi")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Bitiş Tarihi")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Devam Ediyor")]
        public bool IsCurrent { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Sıralama negatif olamaz.")]
        [Display(Name = "Gösterim Sırası")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Aktif Mi?")]
        public bool IsActive { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (IsCurrent && EndDate.HasValue)
            {
                yield return new ValidationResult(
                    "Devam eden eğitimlerde bitiş tarihi boş olmalıdır.",
                    new[] { nameof(EndDate) });
            }

            if (StartDate.HasValue &&
                EndDate.HasValue &&
                EndDate.Value.Date < StartDate.Value.Date)
            {
                yield return new ValidationResult(
                    "Bitiş tarihi başlangıç tarihinden önce olamaz.",
                    new[] { nameof(EndDate) });
            }
        }
    }
}
