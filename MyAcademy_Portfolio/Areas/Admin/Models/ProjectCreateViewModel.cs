
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class ProjectCreateViewModel
    {
        [Required(ErrorMessage = "Proje adı zorunludur.")]
        [StringLength(150, ErrorMessage = "Proje adı en fazla 150 karakter olabilir.")]
        [Display(Name = "Proje Adı")]
        public string ProjectTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Proje açıklaması zorunludur.")]
        [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
        [Display(Name = "Proje Açıklaması")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
        [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir kategori seçiniz.")]
        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }

        [Display(Name = "Kullanılan Teknolojiler")]
        public string? Technologies { get; set; }

        [Url(ErrorMessage = "Geçerli bir GitHub bağlantısı giriniz.")]
        [Display(Name = "GitHub Adresi")]
        public string? GithubUrl { get; set; }

        [Url(ErrorMessage = "Geçerli bir canlı proje bağlantısı giriniz.")]
        [Display(Name = "Canlı Proje Adresi")]
        public string? LiveUrl { get; set; }

        [Display(Name = "Proje Görseli")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Aktif Mi?")]
        public bool IsActive { get; set; } = true;

        public List<SelectListItem> Categories { get; set; } = new();
    }
}
