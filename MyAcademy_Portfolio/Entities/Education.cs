namespace MyAcademy_PortfolioProject.Entities
{
    public class Education
    {
        public int EducationId { get; set; }

        public string? Title { get; set; }

        public string? Institution { get; set; }

        public string? Description { get; set; }

        public string? EducationType { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsCurrent { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}