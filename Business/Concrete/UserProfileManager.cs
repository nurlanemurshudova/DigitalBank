using Business.Abstract;
using Core.Results.Abstract;
using Core.Results.Concrete;
using Entities.Concrete.Dtos;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Business.Concrete
{
    public class UserProfileManager : IUserProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserProfileManager(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IResult> UpdateProfileAsync(int userId, UpdateProfileDto model)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new ErrorResult("İstifadəçi tapılmadı");
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Address = model.Address;
            user.Age = model.Age;
            user.UpdatedDate = DateTime.Now;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ErrorResult($"Profil yenilənmədi: {errors}");
            }

            return new SuccessResult("Profil uğurla yeniləndi");
        }

        public async Task<IDataResult<string>> UploadAvatarAsync(int userId, IFormFile avatar, string webRootPath)
        {
            if (avatar == null || avatar.Length == 0)
            {
                return new ErrorDataResult<string>("Şəkil seçilməyib");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(avatar.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                return new ErrorDataResult<string>("Yalnız JPG, PNG və GIF formatları qəbul edilir");
            }

            if (avatar.Length > 5 * 1024 * 1024)
            {
                return new ErrorDataResult<string>("Şəkil ölçüsü 5MB-dan çox ola bilməz");
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new ErrorDataResult<string>("İstifadəçi tapılmadı");
            }

            try
            {
                var uploadsFolder = Path.Combine(webRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }


                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var oldAvatarPath = Path.Combine(webRootPath, user.AvatarUrl.TrimStart('/'));
                    if (File.Exists(oldAvatarPath))
                    {
                        File.Delete(oldAvatarPath);
                    }
                }


                var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                var avatarUrl = $"/uploads/avatars/{fileName}";
                user.AvatarUrl = avatarUrl;
                user.UpdatedDate = DateTime.Now;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return new ErrorDataResult<string>("Şəkil yüklənmədi");
                }

                return new SuccessDataResult<string>(avatarUrl, "Şəkil uğurla yükləndi");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<string>($"Xəta: {ex.Message}");
            }
        }

        public async Task<IResult> DeleteAvatarAsync(int userId, string webRootPath)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new ErrorResult("İstifadəçi tapılmadı");
            }

            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var avatarPath = Path.Combine(webRootPath, user.AvatarUrl.TrimStart('/'));
                if (File.Exists(avatarPath))
                {
                    File.Delete(avatarPath);
                }

                user.AvatarUrl = null;
                user.UpdatedDate = DateTime.Now;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return new ErrorResult("Şəkil silinmədi");
                }
            }

            return new SuccessResult("Şəkil silindi");
        }

        public async Task<IResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new ErrorResult("İstifadəçi tapılmadı");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ErrorResult(errors);
            }

            return new SuccessResult("Şifrə uğurla dəyişdirildi");
        }
    }
}