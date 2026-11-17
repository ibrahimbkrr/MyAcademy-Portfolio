
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyAcademy_PortfolioProject.Areas.Admin.Models;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // ==================== LOGIN GET ====================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true &&
                User.IsInRole("Admin"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Admin" });
            }

            return View(new LoginViewModel());
        }

        // ==================== LOGIN POST ====================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(
                model.Email.Trim());

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "E-posta veya parola hatalı.");

                return View(model);
            }

            var result = await _signInManager
                .CheckPasswordSignInAsync(
                    user,
                    model.Password,
                    lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Hesap geçici olarak kilitlendi.");
                return View(model);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "E-posta veya parola hatalı.");
                return View(model);
            }

            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                _logger.LogWarning(
                    "Admin rolü olmayan hesapla yönetim paneline giriş denemesi.");

                ModelState.AddModelError(
                    string.Empty,
                    "Bu hesabın yönetim paneline erişim yetkisi bulunmuyor.");

                return View(model);
            }

            await _signInManager.SignInAsync(
                user,
                isPersistent: model.RememberMe);

            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Admin" });
        }

        // ==================== LOGOUT ====================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(nameof(Login));
        }

        // ==================== ACCESS DENIED ====================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
