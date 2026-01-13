using Business.Utilities;
using Entities.Concrete.Dtos.Membership;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace DigitalBankUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(Login model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var apiUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7002";

                var json = JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync($"{apiUrl}/api/Auth/user/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (result.TryGetProperty("token", out JsonElement tokenElement))
                    {
                        var token = tokenElement.GetString();

                        Response.Cookies.Append("AuthToken", token, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Lax, 
                            Expires = DateTimeOffset.UtcNow.AddHours(24) 
                        });

                        return RedirectToAction("Index", "Home");
                    }
                }

                ViewBag.Error = "Email və ya şifrə yanlışdır";
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Xəta baş verdi: " + ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        [Authorize]
        public IActionResult Logout()
        {

            Response.Cookies.Delete("AdminAuthToken");
            Response.Cookies.Delete("AuthToken");

            //HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            //HttpContext.Response.Headers["Pragma"] = "no-cache";
            //HttpContext.Response.Headers["Expires"] = "0";


            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(Register model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ViewBag.Error = "Bu email artıq qeydiyyatdan keçib.";
                return View(model);
            }

            var passwordValidator = HttpContext.RequestServices.GetRequiredService<IPasswordValidator<ApplicationUser>>();
            var fakeUser = new ApplicationUser { UserName = model.Email, Email = model.Email };
            var passwordCheck = await passwordValidator.ValidateAsync(_userManager, fakeUser, model.Password);

            if (!passwordCheck.Succeeded)
            {
                foreach (var error in passwordCheck.Errors)
                {
                    ModelState.AddModelError("Password", error.Description);
                }
                return View(model);
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
                AvatarUrl = model.AvatarUrl
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                TempData["Success"] = "Qeydiyyat uğurla tamamlandı.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}