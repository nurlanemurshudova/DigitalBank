using Core.Results.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Abstract
{
    public interface IStripeService
    {
        Task<IDataResult<string>> CreateCheckoutSessionAsync(int userId, decimal amount, string successUrl, string cancelUrl);

        Task<IResult> ProcessWebhookEventAsync(string json, string signature, string webhookSecret);
    }
}
