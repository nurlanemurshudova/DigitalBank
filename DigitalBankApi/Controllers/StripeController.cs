using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Business.Abstract;
using Entities.Concrete.TableModels;
using DigitalBankApi.Models;
using Entities.Concrete.Dtos;

namespace DigitalBankApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StripeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IStripeService _stripeService;

        public StripeController(
            IConfiguration configuration,
            IStripeService stripeService)
        {
            _configuration = configuration;
            _stripeService = stripeService;

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        [HttpPost("create-checkout-session")]
        [Authorize] 
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutRequestDto request)
        {
            var frontendUrl = _configuration["AppSettings:FrontendUrl"];
            var successUrl = $"{frontendUrl}/Home/PaymentSuccess?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{frontendUrl}/Home/PaymentCancel";

            var result = await _stripeService.CreateCheckoutSessionAsync(
                request.UserId,
                request.Amount,
                successUrl,
                cancelUrl
            );

            if (!result.IsSuccess)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new
            {
                success = true,
                url = result.Data,
                message = result.Message
            });
        }

        [HttpPost("webhook")]
        [AllowAnonymous] 
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();
            var webhookSecret = _configuration["Stripe:WebhookSecret"];

            var result = await _stripeService.ProcessWebhookEventAsync(
                json,
                signature,
                webhookSecret
            );

            if (!result.IsSuccess)
            {
                return BadRequest(new { error = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }
}