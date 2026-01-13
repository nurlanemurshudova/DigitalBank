using Business.Utilities;
using Entities.Concrete.Dtos.Membership;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalBankApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("user/login")]
        public async Task<IActionResult> UserLogin([FromBody] Login model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Model validation failed", errors = ModelState });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized(new { message = "Email və ya şifrə yanlışdır" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin") || roles.Contains("SubAdmin"))
            {
                return BadRequest(new { message = "Bu hesabla giriş edilə bilməz" });
            }

            var token = JwtHelper.GenerateToken(user, roles.ToList(), _configuration);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.AccountNumber,
                    user.Balance,
                    user.Address,
                    user.Age
                },
                roles
            });
        }


        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] Login model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Model validation failed", errors = ModelState });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized(new { message = "Email və ya şifrə yanlışdır" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin") && !roles.Contains("SubAdmin"))
            {
                return Unauthorized(new { message = "Admin icazəniz yoxdur" });
            }

            var token = JwtHelper.GenerateToken(user, roles.ToList(), _configuration);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName
                },
                roles
            });
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Register model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Model validation failed", errors = ModelState });
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Bu email artıq qeydiyyatdan keçib" });
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
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Qeydiyyat uğursuz",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            await _userManager.AddToRoleAsync(user, "User");

            var roles = await _userManager.GetRolesAsync(user);
            var token = JwtHelper.GenerateToken(user, roles.ToList(), _configuration);

            return Ok(new
            {
                message = "Qeydiyyat uğurla tamamlandı",
                token,
                user = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.AccountNumber,
                    user.Balance,
                    user.Address,
                    user.Age
                }
            });
        }
    }
}