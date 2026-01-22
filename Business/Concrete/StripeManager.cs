using Business.Abstract;
using Core.Results.Abstract;
using Core.Results.Concrete;
using DataAccess.UnitOfWork;
using Entities.Concrete.TableModels;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Identity;
using Stripe;
using Stripe.Checkout;

namespace Business.Concrete
{
    public class StripeManager : IStripeService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public StripeManager(
            UserManager<ApplicationUser> userManager,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }


        public async Task<IDataResult<string>> CreateCheckoutSessionAsync(
            int userId,
            decimal amount,
            string successUrl,
            string cancelUrl)
        {
            if (amount <= 0)
            {
                return new ErrorDataResult<string>("Məbləğ 0-dan böyük olmalıdır");
            }

            if (userId <= 0)
            {
                return new ErrorDataResult<string>("User ID tələb olunur");
            }

            
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new ErrorDataResult<string>("İstifadəçi tapılmadı");
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
                                    Description = $"{amount} AZN hesaba əlavə ediləcək"
                                },
                                UnitAmount = (long)(amount * 100) 
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,

                    
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_id", userId.ToString() },
                        { "amount", amount.ToString() },
                        { "user_email", user.Email }
                    }
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return new SuccessDataResult<string>(
                    session.Url,
                    "Checkout session yaradıldı"
                );
            }
            catch (StripeException ex)
            {
                return new ErrorDataResult<string>($"Stripe xətası: {ex.Message}");
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<string>($"Sistem xətası: {ex.Message}");
            }
        }


        public async Task<IResult> ProcessWebhookEventAsync(
            string json,
            string signature,
            string webhookSecret)
        {
            if (string.IsNullOrEmpty(webhookSecret))
            {
                return new ErrorResult("Webhook secret konfiqurasiyada tapılmadı");
            }

            try
            {

                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signature,
                    webhookSecret
                );

                
                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;

                    if (session != null && session.PaymentStatus == "paid")
                    {
                        return await ProcessSuccessfulPaymentAsync(session);
                    }
                }

                return new SuccessResult("Webhook event işləndi");
            }
            catch (StripeException ex)
            {
                return new ErrorResult($"Stripe xətası: {ex.Message}");
            }
            catch (Exception ex)
            {
                return new ErrorResult($"Webhook xətası: {ex.Message}");
            }
        }

        private async Task<IResult> ProcessSuccessfulPaymentAsync(Session session)
        {
            try
            {

                if (!session.Metadata.TryGetValue("user_id", out var userIdStr) ||
                    !session.Metadata.TryGetValue("amount", out var amountStr))
                {
                    return new ErrorResult("Metadata-da user_id və ya amount tapılmadı");
                }

                var userId = int.Parse(userIdStr);
                var amount = decimal.Parse(amountStr);


                await _unitOfWork.BeginTransactionAsync();


                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return new ErrorResult("İstifadəçi tapılmadı");
                }

                
                user.Balance += amount;
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    await _unitOfWork.RollbackAsync();
                    return new ErrorResult("Balans yenilənmədi");
                }

                
                var transaction = new Transaction
                {
                    SenderId = userId,     
                    ReceiverId = userId,   
                    Amount = amount,
                    Description = "Balans artırma",
                    Status = TransactionStatus.Success,
                    CreatedDate = DateTime.Now
                };

                await _unitOfWork.Repository<Transaction>().AddAsync(transaction);

                
                await _unitOfWork.CommitTransactionAsync();

                return new SuccessResult(
                    $"User {userId} balansı {amount} AZN artırıldı. Yeni balans: {user.Balance} AZN"
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return new ErrorResult($"Ödəniş işlənərkən xəta: {ex.Message}");
            }
        }
    }
}
