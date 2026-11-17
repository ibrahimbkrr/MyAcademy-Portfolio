
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace MyAcademy_PortfolioProject.Conventions
{
    public class AdminAuthorizationConvention
        : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            var adminPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole("Admin")
                .Build();

            foreach (var controller in application.Controllers)
            {
                bool isAdminArea = controller.Attributes
                    .OfType<AreaAttribute>()
                    .Any(x => x.RouteValue == "Admin");

                if (isAdminArea)
                {
                    controller.Filters.Add(
                        new AuthorizeFilter(adminPolicy));
                }
            }
        }
    }
}
