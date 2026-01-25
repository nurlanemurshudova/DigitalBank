using Business.Abstract;
using Entities.Concrete.Dtos;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Update.Internal;
using System.Security.Claims;

namespace DigitalBankUI.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin,SubAdmin")]
    public class SettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserProfileService _profileService;
        private readonly IWebHostEnvironment _environment;

        public SettingsController(
            UserManager<ApplicationUser> userManager,
            IUserProfileService profileService,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _profileService = profileService;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "Dashboard" });

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            
            var result = await _profileService.UpdateProfileAsync(
                userId,
                new UpdateProfileDto
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName
                }
            );

            TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            
            var result = await _profileService.UploadAvatarAsync(
                userId,
                avatar,
                _environment.WebRootPath
            );

            TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvatar()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            
            var result = await _profileService.DeleteAvatarAsync(userId, _environment.WebRootPath);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            {
                TempData["Error"] = "Bütün şifrə xanaları doldurulmalıdır";
                return RedirectToAction("Index");
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Yeni şifrələr uyğun gəlmir";
                return RedirectToAction("Index");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            
            var result = await _profileService.ChangePasswordAsync(
                userId,
                currentPassword,
                newPassword
            );

            TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Index");
        }
    }
}