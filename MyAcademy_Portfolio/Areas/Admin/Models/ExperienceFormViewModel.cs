
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class ExperienceFormViewModel : IValidatableObject
    {
        public int ExperienceId { get; set; }

        [Required(ErrorMessage = "Deneyim başlığı zorunludur.")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şirket veya kurum adı zorunludur.")]
        [StringLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Deneyim türü zorunludur.")]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public bool IsCurrent { get; set; }

        public bool IsActive { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (IsCurrent && EndDate.HasValue)
            {
                yield return new ValidationResult(
                    "Devam eden deneyimlerde bitiş tarihi boş olmalıdır.",
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
