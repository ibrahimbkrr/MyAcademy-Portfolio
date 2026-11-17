
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyAcademy_PortfolioProject.Migrations
{
    public partial class mig3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // mig1 used the misspelled Greating name. Preserve its data on a clean install,
            // while accepting databases where the column was already renamed manually.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'HeroSections', N'Greating') IS NOT NULL
                   AND COL_LENGTH(N'HeroSections', N'Greeting') IS NULL
                    EXEC sp_rename N'HeroSections.Greating', N'Greeting', N'COLUMN';
                """);
            // HeroSections tablosundaki mevcut sütunları
            // nullable hale getiriyoruz.

            migrationBuilder.AlterColumn<string>(
                name: "Greeting",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "MainImageUrl",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: false);

            // Categories tablosunu oluşturuyoruz.

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    CategoryName = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Categories",
                        x => x.CategoryId);
                });

            // Projects tablosunu oluşturuyoruz.

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    ProjectTitle = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    Description = table.Column<string>(
                        type: "nvarchar(max)",
                        nullable: false),

                    CategoryId = table.Column<int>(
                        type: "int",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Projects",
                        x => x.ProjectId);

                    table.ForeignKey(
                        name: "FK_Projects_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Projects ve Categories ilişkisi için index.

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CategoryId",
                table: "Projects",
                column: "CategoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Projects tablosunu kaldırıyoruz.

            migrationBuilder.DropTable(
                name: "Projects");

            // Categories tablosunu kaldırıyoruz.

            migrationBuilder.DropTable(
                name: "Categories");

            // HeroSections sütunlarını eski haline getiriyoruz.

            migrationBuilder.AlterColumn<string>(
                name: "Greeting",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'HeroSections', N'Greeting') IS NOT NULL
                   AND COL_LENGTH(N'HeroSections', N'Greating') IS NULL
                    EXEC sp_rename N'HeroSections.Greeting', N'Greating', N'COLUMN';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MainImageUrl",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "HeroSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
