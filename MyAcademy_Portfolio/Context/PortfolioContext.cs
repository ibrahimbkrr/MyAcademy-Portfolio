
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Context
{
    public class PortfolioContext : IdentityDbContext<AppUser>
    {
        public PortfolioContext(
            DbContextOptions<PortfolioContext> options)
            : base(options)
        {
        }

        // HERO
        public DbSet<HeroSection> HeroSections { get; set; }
        public DbSet<HeroTitle> HeroTitles { get; set; }

        // ABOUT
        public DbSet<About> Abouts { get; set; }

        // EXPERIENCE
        public DbSet<Experience> Experiences { get; set; }

        // SKILLS
        public DbSet<Skill> Skills { get; set; }
        public DbSet<SkillItem> SkillItems { get; set; }
        public DbSet<SkillCategory> SkillCategories { get; set; }

        // PROJECTS
        public DbSet<Project> Projects { get; set; }
        public DbSet<Category> Categories { get; set; }

        // EDUCATION
        public DbSet<Education> Educations { get; set; }
        public DbSet<EducationContent> EducationContents { get; set; }

        // PHILOSOPHY
        public DbSet<Philosophy> Philosophies { get; set; }

        // CONTACT
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Message> Messages { get; set; }

        // SOCIAL MEDIA
        public DbSet<SocialMedia> SocialMedias { get; set; }

        // FOOTER
        public DbSet<Footer> Footers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Match the existing category validators; other displayed titles need not be unique.
            builder.Entity<Category>().HasIndex(x => x.CategoryName).IsUnique();
            builder.Entity<Category>().HasIndex(x => x.FilterKey).IsUnique();
            builder.Entity<SkillCategory>().HasIndex(x => x.CategoryName).IsUnique();
        }
    }
}
