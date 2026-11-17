
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MyAcademy_PortfolioProject.Areas.Admin.Models
{
    public class AboutFormViewModel
    {
        public int AboutId { get; set; }

        // PROFİL BİLGİLERİ

        [Required(ErrorMessage = "İsim zorunludur.")]
        [StringLength(150)]
        [Display(Name = "Ad Soyad")]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Konum")]
        public string? Location { get; set; }

        [StringLength(150)]
        [Display(Name = "Meslek / Unvan")]
        public string? JobTitle { get; set; }

        // HAKKIMDA BÖLÜMÜ

        [StringLength(150)]
        [Display(Name = "Bölüm Etiketi")]
        public string? SectionLabel { get; set; }

        [Required(ErrorMessage = "Bölüm başlığı zorunludur.")]
        [StringLength(250)]
        [Display(Name = "Bölüm Başlığı")]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        [Display(Name = "Birinci Açıklama")]
        public string? Description1 { get; set; }

        [StringLength(4000)]
        [Display(Name = "İkinci Açıklama")]
        public string? Description2 { get; set; }

        // İSTATİSTİKLER

        [StringLength(50)]
        public string? Stat1Value { get; set; }

        [StringLength(100)]
        public string? Stat1Label { get; set; }

        [StringLength(50)]
        public string? Stat2Value { get; set; }

        [StringLength(100)]
        public string? Stat2Label { get; set; }

        [StringLength(50)]
        public string? Stat3Value { get; set; }

        [StringLength(100)]
        public string? Stat3Label { get; set; }

        // MEVCUT DOSYA ADRESLERİ
        // Bunları sadece önizleme amacıyla kullanacağız.

        public string? ExistingImageUrl { get; set; }

        public string? ExistingCvUrl { get; set; }

        // YENİ DOSYA YÜKLEMELERİ

        [Display(Name = "Profil Fotoğrafı")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "CV Dosyası")]
        public IFormFile? CvFile { get; set; }
    }
}
