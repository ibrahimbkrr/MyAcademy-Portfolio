using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Entities
{
    public class Contact
    {
        public int ContactId { get; set; }

        [Required]
        [StringLength(100)]
        public string SectionLabel { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Location { get; set; }

        public string? PhoneNumber { get; set; }
    }
}