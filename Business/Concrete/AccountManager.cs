using Business.Abstract;
using Business.Utilities;
using Core.Results.Abstract;
using Core.Results.Concrete;
using Entities.Concrete.Dtos.Membership;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Business.Concrete
{
    public class AccountManager : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        public AccountManager(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<IResult> RegisterUserAsync(Register model)
        {
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                return new ErrorResult("Bu email artıq qeydiyyatdan keçib");
            }

            var passwordValidation = await ValidatePasswordAsync(model.Email, model.Password);
            if (!passwordValidation.IsSuccess)
            {
                return passwordValidation;
            }

            string accountNumber;
            do
            {
                accountNumber = AccountNumberHelper.GenerateAccountNumber();
            }
            while (await _userManager.Users.AnyAsync(u => u.AccountNumber == accountNumber));

            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = model.Address,
                Age = model.Age,
                Balance = 0,
                Email = model.Email,
                UserName = model.Email,
                AccountNumber = accountNumber,
                EmailConfirmed = true,
                AvatarUrl = model.AvatarUrl,
                RegisterDate = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new ErrorResult($"Qeydiyyat uğursuz: {errors}");
            }

            await _userManager.AddToRoleAsync(user, "User");

            return new SuccessResult("Qeydiyyat uğurla tamamlandı");
        }

        private async Task<IResult> ValidatePasswordAsync(string email, string password)
        {
            var fakeUser = new ApplicationUser { UserName = email, Email = email };

            var passwordValidators = _userManager.PasswordValidators;

            foreach (var validator in passwordValidators)
            {
                var result = await validator.ValidateAsync(_userManager, fakeUser, password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return new ErrorResult(errors);
                }
            }

            return new SuccessResult("Şifrə düzgündür");
        }



        public async Task<IDataResult<LoginResponse>> UserLoginAsync(Login model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return new ErrorDataResult<LoginResponse>(default, "Email və ya şifrə yanlışdır");

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any(r => r == "Admin" || r == "SubAdmin"))
                return new ErrorDataResult<LoginResponse>(default, "Bu hesabla user panelə giriş edilə bilməz");

            return new SuccessDataResult<LoginResponse>(CreateResponse(user, roles.ToList()), "Giriş uğurlu");
        }

        public async Task<IDataResult<LoginResponse>> AdminLoginAsync(Login model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return new ErrorDataResult<LoginResponse>(default, "Email və ya şifrə yanlışdır");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => r == "Admin" || r == "SubAdmin"))
                return new ErrorDataResult<LoginResponse>(default, "Admin panelə giriş icazəniz yoxdur");

            return new SuccessDataResult<LoginResponse>(CreateResponse(user, roles.ToList()), "Admin girişi uğurlu");
        }

        private LoginResponse CreateResponse(ApplicationUser user, List<string> roles)
        {
            return new LoginResponse
            {
                Token = JwtHelper.GenerateToken(user, roles, _configuration),
                Roles = roles,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    AccountNumber = user.AccountNumber,
                    Balance = user.Balance,
                    Address = user.Address,
                    Age = user.Age
                }
            };
        }
    }
}