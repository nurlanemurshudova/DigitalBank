namespace DigitalBankApi.Models
{
    public class CheckoutRequest
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
    }
}
