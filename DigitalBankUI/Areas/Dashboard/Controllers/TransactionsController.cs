using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBankUI.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "Admin, SubAdmin")]
    public class TransactionsController : Controller
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        public async Task<IActionResult> Index(string filter = "all")
        {
            DateTime? startDate = null;
            DateTime? endDate = DateTime.Now;

            switch (filter.ToLower())
            {
                case "today":
                    startDate = DateTime.Today;
                    ViewBag.FilterName = "Bu gün";
                    break;
                case "week":
                    startDate = DateTime.Today.AddDays(-7);
                    ViewBag.FilterName = "Bu həftə";
                    break;
                case "month":
                    startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    ViewBag.FilterName = "Bu ay";
                    break;
                default:
                    ViewBag.FilterName = "Bütün vaxtlar";
                    break;
            }

            var result = startDate.HasValue
                ? await _transactionService.GetTransactionsByDateAsync(startDate, endDate)
                : await _transactionService.GetAllTransactionsAsync();

            ViewBag.CurrentFilter = filter;
            ViewBag.TotalAmount = result.Data.Sum(t => t.Amount);
            ViewBag.TotalCount = result.Data.Count;

            return View(result.Data);
        }


        [HttpGet]
        public async Task<IActionResult> FilterByDate(DateTime startDate, DateTime endDate)
        {
            var result = await _transactionService.GetTransactionsByDateAsync(startDate, endDate);

            ViewBag.FilterName = $"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
            ViewBag.CurrentFilter = "custom";
            ViewBag.TotalAmount = result.Data.Sum(t => t.Amount);
            ViewBag.TotalCount = result.Data.Count;
            ViewBag.StartDate = startDate.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.ToString("yyyy-MM-dd");

            return View("Index", result.Data);
        }


    }
}
