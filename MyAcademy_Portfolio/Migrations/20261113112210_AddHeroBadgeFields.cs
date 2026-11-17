using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAcademy_PortfolioProject.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroBadgeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BottomBadgeText",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BottomBadgeValue",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopBadgeText",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TopBadgeValue",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BottomBadgeText",
                table: "HeroSections");

            migrationBuilder.DropColumn(
                name: "BottomBadgeValue",
                table: "HeroSections");

            migrationBuilder.DropColumn(
                name: "TopBadgeText",
                table: "HeroSections");

            migrationBuilder.DropColumn(
                name: "TopBadgeValue",
                table: "HeroSections");
        }
    }
}
