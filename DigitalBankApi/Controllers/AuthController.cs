using Business.Abstract;
using Entities.Concrete.Dtos.Membership;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBankApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAccountService _authService;

        public AuthController(IAccountService authService)
        {
            _authService = authService;
        }

        [HttpPost("user/login")]
        public async Task<IActionResult> UserLogin([FromBody] Login model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                { success = false, message = "Model validation failed", errors = ModelState });
            }
            var result = await _authService.UserLoginAsync(model);
            if (!result.IsSuccess)
            {
                return Unauthorized(new
                { success = false, message = result.Message });
            }
            return Ok(new { success = true, token = result.Data.Token, user = result.Data.User, roles = result.Data.Roles, message = result.Message });
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] Login model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Model validation failed", errors = ModelState });
            }
            var result = await _authService.AdminLoginAsync(model);
            if (!result.IsSuccess)
            {
                return Unauthorized(new { success = false, message = result.Message });
            }
            return Ok(new { success = true, token = result.Data.Token, user = result.Data.User, roles = result.Data.Roles, message = result.Message });
        }
    }
}