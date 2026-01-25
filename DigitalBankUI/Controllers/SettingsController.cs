using Business.Abstract;
using Entities.Concrete.Dtos;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DigitalBankUI.Controllers
{
    [Authorize]
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
                return RedirectToAction("Login", "Account");

            return View(user);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Məlumatlar düzgün deyil";
                return RedirectToAction("Index");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);


            var result = await _profileService.UpdateProfileAsync(userId, model);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Index");
        }


        [HttpPost]
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
        public async Task<IActionResult> DeleteAvatar()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _profileService.DeleteAvatarAsync(userId, _environment.WebRootPath);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Index");
        }


        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Yeni şifrə və təkrarı uyğun gəlmir");
                return View(model);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var result = await _profileService.ChangePasswordAsync(
                userId,
                model.CurrentPassword,
                model.NewPassword
            );

            if (result.IsSuccess)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", result.Message);
            return View(model);
        }
    }
}