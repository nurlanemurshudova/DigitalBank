using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Business.Abstract;
using Entities.Concrete.TableModels;
using DigitalBankApi.Models;

namespace DigitalBankApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StripeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITransactionService _transactionService; 

        public StripeController(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ITransactionService transactionService) 
        {
            _configuration = configuration;
            _userManager = userManager;
            _transactionService = transactionService; 

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        [HttpPost("create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Məbləğ 0-dan böyük olmalıdır" });
            }

            if (request.UserId <= 0)
            {
                return BadRequest(new { success = false, message = "User ID tələb olunur" });
            }

            try
            {
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = "Balans artırma",
                                    Description = $"{request.Amount} AZN hesaba əlavə ediləcək"
                                },
                                UnitAmount = (long)(request.Amount * 100)
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = $"{_configuration["AppSettings:FrontendUrl"]}/Home/PaymentSuccess?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = $"{_configuration["AppSettings:FrontendUrl"]}/Home/PaymentCancel",
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", request.UserId.ToString() },
                        { "amount", request.Amount.ToString() }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return Ok(new
                {
                    success = true,
                    sessionId = session.Id,
                    url = session.Url
                });
            }
            catch (StripeException ex)
            {
                return BadRequest(new { success = false, message = $"Stripe xətası: {ex.Message}" });
            }
        }


        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"];

                if (string.IsNullOrEmpty(webhookSecret))
                    throw new Exception("WebhookSecret konfiqurasiyada tapılmadı!");

                var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;

                    if (session != null && session.PaymentStatus == "paid")
                    {
                        var userId = int.Parse(session.Metadata["user_id"]);
                        var amount = decimal.Parse(session.Metadata["amount"]);

                        var user = await _userManager.FindByIdAsync(userId.ToString());
                        if (user != null)
                        {

                            user.Balance += amount;
                            await _userManager.UpdateAsync(user);

                            var transaction = new Transaction
                            {
                                SenderId = userId,     
                                ReceiverId = userId,      
                                Amount = amount,
                                Description = $"Balans artırma",
                                Status = TransactionStatus.Success
                            };

                            await _transactionService.AddAsync(transaction);

                        }
                    }
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}