
using Microsoft.AspNetCore.Identity;

namespace MyAcademy_PortfolioProject.Entities
{
    public class AppUser : IdentityUser
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }
    }
}
