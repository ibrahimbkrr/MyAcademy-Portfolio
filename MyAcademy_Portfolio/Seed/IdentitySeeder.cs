
using Microsoft.AspNetCore.Identity;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Seed
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            IServiceProvider services,
            IConfiguration configuration)
        {
            using var scope = services.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<AppUser>>();

            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            const string adminRole = "Admin";

            // Admin rolünü oluştur
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                var roleResult = await roleManager.CreateAsync(
                    new IdentityRole(adminRole));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Admin rolü oluşturulamadı: " +
                        string.Join(", ",
                            roleResult.Errors.Select(e => e.Code)));
                }
            }

            // Başlangıç hesabı bilgilerini güvenli yapılandırmadan oku
            var email = configuration["AdminSeed:Email"];
            var password = configuration["AdminSeed:Password"];

            // Bilgiler tanımlı değilse kullanıcı oluşturma
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            email = email.Trim();

            var user = await userManager.FindByEmailAsync(email);

            // A matching configured email must never elevate an existing account.
            if (user != null)
            {
                if (await userManager.IsInRoleAsync(user, adminRole))
                    return;

                throw new InvalidOperationException(
                    "Admin seed refused: the configured account already exists without the Admin role. Review the account manually.");
            }

            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User"
                };

                var result = await userManager.CreateAsync(
                    user, password);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Admin kullanıcısı oluşturulamadı: " +
                        string.Join(", ",
                            result.Errors.Select(e => e.Code)));
                }
            }

            // Hesapta Admin rolü yoksa ata
            if (!await userManager.IsInRoleAsync(user, adminRole))
            {
                var roleResult = await userManager.AddToRoleAsync(
                    user, adminRole);

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Admin rolü kullanıcıya atanamadı: " +
                        string.Join(", ",
                            roleResult.Errors.Select(e => e.Code)));
                }
            }
        }
    }
}
