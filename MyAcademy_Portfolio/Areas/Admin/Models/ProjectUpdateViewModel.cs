using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class ProjectUpdateViewModel
    {
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Proje adı zorunludur.")]
        [StringLength(150)]
        public string ProjectTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Proje açıklaması zorunludur.")]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Kategori seçiniz.")]
        public int CategoryId { get; set; }

        public string? Technologies { get; set; }

        [Url(ErrorMessage = "Geçerli bir GitHub bağlantısı giriniz.")]
        public string? GithubUrl { get; set; }

        [Url(ErrorMessage = "Geçerli bir canlı proje bağlantısı giriniz.")]
        public string? LiveUrl { get; set; }

        public string? ExistingImageUrl { get; set; }

        public IFormFile? ImageFile { get; set; }

        public bool IsActive { get; set; }

        public List<SelectListItem> Categories { get; set; } = new();
    }
}