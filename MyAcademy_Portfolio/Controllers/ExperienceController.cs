using Microsoft.AspNetCore.Mvc;

namespace MyAcademy_PortfolioProject.Controllers
{
    public class ExperienceController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Default", fragment: "experience");
        }
    }
}
