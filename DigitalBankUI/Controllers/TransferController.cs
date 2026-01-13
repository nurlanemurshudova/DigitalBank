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
            var result = await _transactionService.GetUserTransactionsAsync(userId);

            if (!result.IsSuccess)
            {
                TempData["Error"] = "Export zamanı xəta baş verdi";
                return RedirectToAction("History");
            }

            var transactions = result.Data;

            if (startDate.HasValue)
                transactions = transactions.Where(t => t.CreatedDate >= startDate.Value).ToList();

            if (endDate.HasValue)
                transactions = transactions.Where(t => t.CreatedDate <= endDate.Value.AddDays(1)).ToList();

            var user = await _userManager.FindByIdAsync(userId.ToString());

            using (var ms = new MemoryStream())
            {
                var document = new iTextSharp.text.Document(PageSize.A4, 25, 25, 30, 30);
                PdfWriter.GetInstance(document, ms);
                document.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var title = new Paragraph("Transaction Report", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                document.Add(title);

                document.Add(new Paragraph(" "));

                // User info
                var infoFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                document.Add(new Paragraph($"User: {user.FirstName} {user.LastName}", infoFont));
                document.Add(new Paragraph($"Account: {user.AccountNumber}", infoFont));
                document.Add(new Paragraph($"Date: {DateTime.Now:dd/MM/yyyy HH:mm}", infoFont));

                document.Add(new Paragraph(" "));

                // Table
                var table = new PdfPTable(6);
                table.WidthPercentage = 100;

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
                table.AddCell(new PdfPCell(new Phrase("Date", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Type", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Party", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Account", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Amount", headerFont)));
                table.AddCell(new PdfPCell(new Phrase("Note", headerFont)));

                var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);
                decimal totalAmount = 0;

                foreach (var t in transactions.OrderByDescending(x => x.CreatedDate))
                {
                    var type = t.SenderId == userId ? "SENT" : "RECEIVED";
                    var party = t.SenderId == userId
                        ? $"{t.Receiver.FirstName} {t.Receiver.LastName}"
                        : $"{t.Sender.FirstName} {t.Sender.LastName}";
                    var account = t.SenderId == userId
                        ? t.Receiver.AccountNumber
                        : t.Sender.AccountNumber;

                    table.AddCell(new PdfPCell(new Phrase(t.CreatedDate.ToString("dd/MM/yyyy"), dataFont)));
                    table.AddCell(new PdfPCell(new Phrase(type, dataFont)));
                    table.AddCell(new PdfPCell(new Phrase(party, dataFont)));
                    table.AddCell(new PdfPCell(new Phrase(account, dataFont)));
                    table.AddCell(new PdfPCell(new Phrase($"{t.Amount:N2} AZN", dataFont)));
                    table.AddCell(new PdfPCell(new Phrase(t.Description ?? "-", dataFont)));

                    totalAmount += t.Amount;
                }

                document.Add(table);

                document.Add(new Paragraph(" "));
                var totalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var total = new Paragraph($"Total: {totalAmount:N2} AZN", totalFont);
                total.Alignment = Element.ALIGN_RIGHT;
                document.Add(total);

                document.Close();
                return File(ms.ToArray(), "application/pdf", $"Transactions_{DateTime.Now:yyyyMMdd}.pdf");
            }
        }


        [HttpGet]
        public async Task<IActionResult> ExportExcel(DateTime? startDate, DateTime? endDate)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _transactionService.GetUserTransactionsAsync(userId);

            if (!result.IsSuccess)
            {
                TempData["Error"] = "Export zamanı xəta baş verdi";
                return RedirectToAction("History");
            }

            var transactions = result.Data;

            if (startDate.HasValue)
                transactions = transactions.Where(t => t.CreatedDate >= startDate.Value).ToList();

            if (endDate.HasValue)
                transactions = transactions.Where(t => t.CreatedDate <= endDate.Value.AddDays(1)).ToList();

            var user = await _userManager.FindByIdAsync(userId.ToString());

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Transactions");

                worksheet.Cell(1, 1).Value = "Date";
                worksheet.Cell(1, 2).Value = "Type";
                worksheet.Cell(1, 3).Value = "Party";
                worksheet.Cell(1, 4).Value = "Account";
                worksheet.Cell(1, 5).Value = "Amount (AZN)";
                worksheet.Cell(1, 6).Value = "Note";

                var headerRange = worksheet.Range(1, 1, 1, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 2;
                decimal totalAmount = 0;

                foreach (var t in transactions.OrderByDescending(x => x.CreatedDate))
                {
                    var type = t.SenderId == userId ? "SENT" : "RECEIVED";
                    var party = t.SenderId == userId
                        ? $"{t.Receiver.FirstName} {t.Receiver.LastName}"
                        : $"{t.Sender.FirstName} {t.Sender.LastName}";
                    var account = t.SenderId == userId
                        ? t.Receiver.AccountNumber
                        : t.Sender.AccountNumber;

                    worksheet.Cell(row, 1).Value = t.CreatedDate.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(row, 2).Value = type;
                    worksheet.Cell(row, 3).Value = party;
                    worksheet.Cell(row, 4).Value = account;
                    worksheet.Cell(row, 5).Value = t.Amount;
                    worksheet.Cell(row, 6).Value = t.Description ?? "-";

                    totalAmount += t.Amount;
                    row++;
                }

                row++;
                worksheet.Cell(row, 4).Value = "TOTAL:";
                worksheet.Cell(row, 4).Style.Font.Bold = true;
                worksheet.Cell(row, 5).Value = totalAmount;
                worksheet.Cell(row, 5).Style.Font.Bold = true;

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"Transactions_{DateTime.Now:yyyyMMdd}.xlsx"
                    );
                }
            }
        }
    }
}
