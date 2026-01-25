using Core.Results.Abstract;
using Entities.Concrete.Dtos;
using Microsoft.AspNetCore.Http;

namespace Business.Abstract
{
    public interface IUserProfileService
    {
        Task<IResult> UpdateProfileAsync(int userId, UpdateProfileDto model);

        Task<IDataResult<string>> UploadAvatarAsync(int userId, IFormFile avatar, string webRootPath);

        Task<IResult> DeleteAvatarAsync(int userId, string webRootPath);

        Task<IResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}