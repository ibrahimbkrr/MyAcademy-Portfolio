using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAcademy_PortfolioProject.Migrations
{
    /// <inheritdoc />
    public partial class EnforceCategoryUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preflight only: never merge, rename or truncate existing records.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Categories GROUP BY CategoryName HAVING COUNT(*) > 1)
                    THROW 51010, 'Resolve duplicate project category names before applying this migration.', 1;
                IF EXISTS (SELECT 1 FROM Categories WHERE FilterKey IS NOT NULL GROUP BY FilterKey HAVING COUNT(*) > 1)
                    THROW 51011, 'Resolve duplicate project category filter keys before applying this migration.', 1;
                IF EXISTS (SELECT 1 FROM SkillCategories GROUP BY CategoryName HAVING COUNT(*) > 1)
                    THROW 51012, 'Resolve duplicate skill category names before applying this migration.', 1;
                IF EXISTS (SELECT 1 FROM Categories WHERE DATALENGTH(FilterKey) > 900)
                    THROW 51013, 'A legacy filter key exceeds the safe index length. Review it manually.', 1;
                """);
            migrationBuilder.AlterColumn<string>(
                name: "FilterKey",
                table: "Categories",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_CategoryName",
                table: "SkillCategories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_FilterKey",
                table: "Categories",
                column: "FilterKey",
                unique: true,
                filter: "[FilterKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SkillCategories_CategoryName",
                table: "SkillCategories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_FilterKey",
                table: "Categories");

            migrationBuilder.AlterColumn<string>(
                name: "FilterKey",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
