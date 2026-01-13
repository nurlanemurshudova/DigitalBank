using Business.Abstract;
using DigitalBankUI.Models;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace DigitalBankUI.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ITransactionService _transactionService; 

        public HomeController(
            UserManager<ApplicationUser> userManager,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ITransactionService transactionService) 
        {
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _transactionService = transactionService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var transactionsResult = await _transactionService.GetUserTransactionsAsync(int.Parse(userId));

            if (transactionsResult.IsSuccess)
            {
                ViewBag.RecentTransactions = transactionsResult.Data
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(5)
                    .ToList();
            }
            else
            {
                ViewBag.RecentTransactions = new List<Entities.Concrete.TableModels.Transaction>();
            }

            return View(user);
        }


        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] PaymentRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Json(new { success = false, message = "Unauthorized - User ID tapılmadı" });
            }

            var userId = int.Parse(userIdClaim);

            try
            {
                var token = Request.Cookies["AuthToken"]; // JWT token
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "Token tapılmadı" });
                }

                var client = _httpClientFactory.CreateClient();

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var apiUrl = _configuration["ApiSettings:BaseUrl"] + "/api/Stripe/create-checkout-session";

                var payload = new { userId = userId, amount = request.Amount };
                var jsonContent = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;

                    if (root.GetProperty("success").GetBoolean())
                    {
                        var checkoutUrl = root.GetProperty("url").GetString();
                        return Json(new { success = true, url = checkoutUrl });
                    }
                }

                return Json(new { success = false, message = $"API xətası: {responseContent}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Sistem xətası: {ex.Message}" });
            }
        }

        [AllowAnonymous] 
        public IActionResult PaymentSuccess(string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
            {
                TempData["Error"] = "Session ID tapılmadı";
                return RedirectToAction("Index", "Home");
            }

            TempData["Success"] = "✅ Ödəniş uğurla tamamlandı!";

            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            TempData.Keep("Success"); 
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        public IActionResult PaymentCancel()
        {
            TempData["Error"] = "❌ Ödəniş ləğv edildi";

            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            TempData.Keep("Error");
            return RedirectToAction("Login", "Account");
        }
    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }
    }
}