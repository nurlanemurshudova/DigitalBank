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
        private readonly IWebHostEnvironment _environment;

        public SettingsController(
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
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
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                TempData["Error"] = "İstifadəçi tapılmadı";
                return RedirectToAction("Index");
            }

            // Update fields
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Address = model.Address;
            user.Age = model.Age;
            user.UpdatedDate = DateTime.Now;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["Success"] = "Profil uğurla yeniləndi";
            }
            else
            {
                TempData["Error"] = "Profil yenilənmədi: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar == null || avatar.Length == 0)
            {
                TempData["Error"] = "Şəkil seçilməyib";
                return RedirectToAction("Index");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(avatar.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Yalnız JPG, PNG və GIF formatları qəbul edilir";
                return RedirectToAction("Index");
            }

            if (avatar.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "Şəkil ölçüsü 5MB-dan çox ola bilməz";
                return RedirectToAction("Index");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                TempData["Error"] = "İstifadəçi tapılmadı";
                return RedirectToAction("Index");
            }

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var oldAvatarPath = Path.Combine(_environment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldAvatarPath))
                    {
                        System.IO.File.Delete(oldAvatarPath);
                    }
                }

                var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                user.AvatarUrl = $"/uploads/avatars/{fileName}";
                user.UpdatedDate = DateTime.Now;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["Success"] = "Şəkil uğurla yükləndi";
                }
                else
                {
                    TempData["Error"] = "Şəkil yüklənmədi";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Xəta: {ex.Message}";
            }

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
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                TempData["Error"] = "İstifadəçi tapılmadı";
                return RedirectToAction("Index");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = "Şifrə uğurla dəyişdirildi";
                return RedirectToAction("Index");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteAvatar()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
            {
                TempData["Error"] = "İstifadəçi tapılmadı";
                return RedirectToAction("Index");
            }

            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var avatarPath = Path.Combine(_environment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                if (System.IO.File.Exists(avatarPath))
                {
                    System.IO.File.Delete(avatarPath);
                }

                user.AvatarUrl = null;
                user.UpdatedDate = DateTime.Now;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["Success"] = "Şəkil silindi";
                }
            }

            return RedirectToAction("Index");
        }
    }
}