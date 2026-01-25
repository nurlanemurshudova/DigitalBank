using Business.Abstract;
using Business.Utilities;
using ClosedXML.Excel;
using Core.Results.Concrete;
using DigitalBankUI.Hubs;
using Entities.Concrete.Dtos;
using Entities.Concrete.TableModels;
using Entities.Concrete.TableModels.Membership;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Stripe;
using System.Drawing;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Xml.Linq;

namespace DigitalBankUI.Controllers
{

    [Authorize(Roles = "User")]
    public class TransferController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHub;
        public TransferController(
            ITransactionService transactionService,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> notificationHub)
        {
            _transactionService = transactionService;
            _userManager = userManager;
            _notificationHub = notificationHub;
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _userManager.FindByIdAsync(userId.ToString());

            ViewBag.UserBalance = user.Balance;
            ViewBag.UserAccountNumber = AccountNumberHelper.FormatAccountNumber(user.AccountNumber);

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Send(string receiverAccountNumber, decimal amount, string note)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));


            var result = await _transactionService.TransferMoneyAsync(
                userId,
                receiverAccountNumber,
                amount,
                note);

            if (result.IsSuccess)
            {
                TempData["Success"] = result.Message;

                if (result is SuccessDataResult<TransferResultDto> dataResult)
                {
                    var transferData = dataResult.Data;

                    try
                    {
                        await _notificationHub.Clients
                            .Group($"user_{transferData.ReceiverId}")
                            .SendAsync("ReceiveNotification", new
                            {
                                type = "received",
                                message = $"{transferData.SenderName} sizə {transferData.Amount:N2} AZN göndərdi",
                                amount = transferData.Amount,
                                sender = transferData.SenderName,
                                newBalance = transferData.ReceiverNewBalance,
                                timestamp = DateTime.Now
                            });

                        await _notificationHub.Clients
                            .Group($"user_{transferData.SenderId}")
                            .SendAsync("ReceiveNotification", new
                            {
                                type = "sent",
                                message = $"{transferData.ReceiverName} üçün {transferData.Amount:N2} AZN göndərildi",
                                amount = transferData.Amount,
                                receiver = transferData.ReceiverName,
                                newBalance = transferData.SenderNewBalance,
                                timestamp = DateTime.Now
                            });
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine($"SignalR error: {ex.Message}");
                    }
                }
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> History(DateTime? startDate, DateTime? endDate)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _transactionService.GetUserTransactionsAsync(userId);

            if (result.IsSuccess)
            {
                var transactions = result.Data;


                if (startDate.HasValue)
                    transactions = transactions.Where(t => t.CreatedDate >= startDate.Value).ToList();

                if (endDate.HasValue)
                    transactions = transactions.Where(t => t.CreatedDate <= endDate.Value.AddDays(1)).ToList();

                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

                return View(transactions);
            }

            TempData["Error"] = "Transaction tarixçəsi yüklənmədi";
            return View(new List<Transaction>());
        }
        [HttpGet]
        public async Task<IActionResult> ExportPDF(DateTime? startDate, DateTime? endDate)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var transactionsResult = await _transactionService.GetUserTransactionsAsync(userId);

            if (!transactionsResult.IsSuccess)
            {
                TempData["Error"] = "Export zamanı xəta baş verdi";
                return RedirectToAction("History");
            }

            var transactions = transactionsResult.Data;

            if (startDate.HasValue)
                transactions = transactions.Where(t => t.CreatedDate >= startDate.Value).ToList();

            if (endDate.HasValue)
                transactions = transactions.Where(t => t.CreatedDate <= endDate.Value.AddDays(1)).ToList();


            var result = await _transactionService.ExportTransactionsToPdfAsync(userId, transactions);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction("History");
            }

            return File(result.Data, "application/pdf", $"Transactions_{DateTime.Now:yyyyMMdd}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(DateTime? startDate, DateTime? endDate)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var transactionsResult = await _transactionService.GetUserTransactionsAsync(userId);

            if (!transactionsResult.IsSuccess)
            {
                TempData["Error"] = "Export zamanı xəta baş verdi";
                return RedirectToAction("History");
            }

            var transactions = transactionsResult.Data;

            if (startDate.HasValue)
                transactions = transactions.Where(t => t.CreatedDate >= startDate.Value).ToList();

            if (endDate.HasValue)
                transactions = transactions.Where(t => t.CreatedDate <= endDate.Value.AddDays(1)).ToList();


            var result = await _transactionService.ExportTransactionsToExcelAsync(userId, transactions);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction("History");
            }

            return File(
                result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Transactions_{DateTime.Now:yyyyMMdd}.xlsx"
            );
        }
    }
}
