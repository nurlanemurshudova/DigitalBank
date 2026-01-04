using Business.Utilities;
using Entities.Concrete.Dtos.Membership;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DigitalBankApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("user/login")]
        public async Task<IActionResult> UserLogin([FromBody] Login model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Email və ya şifrə yanlışdır" });

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
                return BadRequest(new { message = "Admin bu paneldən giriş edə bilməz" });

            var token = JwtHelper.GenerateToken(user, roles.ToList(), _configuration);
            return Ok(new { token, user });
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] Login model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Email və ya şifrə yanlışdır" });

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin") && !roles.Contains("SubAdmin"))
                return Unauthorized(new { message = "Admin icazəniz yoxdur" });

            var token = JwtHelper.GenerateToken(user, roles.ToList(), _configuration);
            return Ok(new { token, user });
        }
    }
}