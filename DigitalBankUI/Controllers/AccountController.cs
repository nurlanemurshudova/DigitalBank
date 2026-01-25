using Business.Abstract;
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
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAccountService _accountService;

        public AccountController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IAccountService accountService)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _accountService = accountService;
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
                return View(model);

            var result = await _accountService.RegisterUserAsync(model);

            if (!result.IsSuccess)
            {
                ViewBag.Error = result.Message;
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Login");
        }
    }
}