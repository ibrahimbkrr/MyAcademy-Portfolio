namespace MyAcademy_PortfolioProject.Entities
{
    public class SkillItem
    {
        public int SkillItemId { get; set; }
        public string Title { get; set; } = string.Empty;

        public int SkillId { get; set; }
        public Skill Skill { get; set; } = null!;

    }
}
