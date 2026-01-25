using Business.Abstract;
using Business.BaseMessages;
using Business.Utilities;
using ClosedXML.Excel;
using Core.Results.Abstract;
using Core.Results.Concrete;
using DataAccess.UnitOfWork;
using Entities.Concrete.Dtos;
using Entities.Concrete.TableModels;
using Entities.Concrete.TableModels.Membership;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Xml.Linq;
using Document = iTextSharp.text.Document;

namespace Business.Concrete
{
    public class TransactionManager : ITransactionService
    {
        private readonly IUnitOfWork _uow;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionManager(IUnitOfWork uow, UserManager<ApplicationUser> userManager)
        {
            _uow = uow;
            _userManager = userManager;
        }
        public async Task<IResult> TransferMoneyAsync(int senderId, string receiverAccountNumber, decimal amount, string description)
        {
            if (amount <= 0)
                return new ErrorResult("Məbləğ 0-dan böyük olmalıdır");

            if (string.IsNullOrWhiteSpace(receiverAccountNumber))
                return new ErrorResult("Hesab nömrəsi daxil edin");

            receiverAccountNumber = receiverAccountNumber.Replace("-", "").Replace(" ", "").Trim();

            if (receiverAccountNumber.Length != 16)
                return new ErrorResult("Hesab nömrəsi 16 rəqəm olmalıdır");

            try
            {
                await _uow.BeginTransactionAsync();

                var sender = await _uow.Repository<ApplicationUser>().GetByIdAsync(senderId);
                if (sender == null)
                {
                    await _uow.RollbackAsync();
                    return new ErrorResult("Göndərən tapılmadı");
                }

                if (sender.AccountNumber == receiverAccountNumber)
                {
                    await _uow.RollbackAsync();
                    return new ErrorResult("Özünüzə pul köçürə bilməzsiniz");
                }

                if (sender.Balance < amount)
                {
                    await _uow.RollbackAsync();
                    return new ErrorResult("Balans kifayət deyil");
                }

                var receiver = await _uow.Repository<ApplicationUser>()
                    .Query()
                    .FirstOrDefaultAsync(u => u.AccountNumber == receiverAccountNumber);

                if (receiver == null)
                {
                    await _uow.RollbackAsync();
                    return new ErrorResult("Alıcı hesab nömrəsi tapılmadı");
                }

                sender.Balance -= amount;
                receiver.Balance += amount;

                _uow.Repository<ApplicationUser>().Update(sender);
                _uow.Repository<ApplicationUser>().Update(receiver);

                var transaction = new Transaction
                {
                    SenderId = senderId,
                    ReceiverId = receiver.Id,
                    Amount = amount,
                    Description = description,
                    Status = TransactionStatus.Success
                };

                await _uow.Repository<Transaction>().AddAsync(transaction);

                var notification = new Notification
                {
                    UserId = receiver.Id,
                    Message = $"{sender.FirstName} {sender.LastName} sizə {amount:N2} AZN göndərdi"
                };

                await _uow.Repository<Notification>().AddAsync(notification);

                await _uow.CommitTransactionAsync();

                return new SuccessDataResult<TransferResultDto>(
                   new TransferResultDto
                   {
                       SenderId = sender.Id,                             
                       SenderName = $"{sender.FirstName} {sender.LastName}",
                       SenderNewBalance = sender.Balance,                 

                       ReceiverId = receiver.Id,
                       ReceiverName = $"{receiver.FirstName} {receiver.LastName}",
                       ReceiverNewBalance = receiver.Balance,           

                       Amount = amount
                   },
                   $"Pul köçürməsi uğurla tamamlandı. {receiver.FirstName} {receiver.LastName}-ə {amount:N2} AZN göndərildi"
               );
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                return new ErrorResult($"Xəta baş verdi: {ex.Message}");
            }
        }

        public async Task<IDataResult<List<Transaction>>> GetUserTransactionsAsync(int userId)
        {
            var transactions = await _uow.Repository<Transaction>()
                .Query()
                .Where(t => t.SenderId == userId || t.ReceiverId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .Select(t => new Transaction
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Description = t.Description,
                    CreatedDate = t.CreatedDate,
                    Status = t.Status,
                    SenderId = t.SenderId,
                    ReceiverId = t.ReceiverId,
                    Sender = new ApplicationUser
                    {
                        FirstName = t.Sender.FirstName,
                        LastName = t.Sender.LastName,
                        AccountNumber = t.Sender.AccountNumber 
                    },
                    Receiver = new ApplicationUser
                    {
                        FirstName = t.Receiver.FirstName,
                        LastName = t.Receiver.LastName,
                        AccountNumber = t.Receiver.AccountNumber 
                    }
                })
                .ToListAsync();

            return new SuccessDataResult<List<Transaction>>(transactions);
        }


        public async Task<IDataResult<List<Transaction>>> GetAllTransactionsAsync()
        {
            var transactions = await _uow.Repository<Transaction>()
                .Query()
                .OrderByDescending(t => t.CreatedDate)
                .Select(t => new Transaction
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    SenderId = t.SenderId,
                    ReceiverId = t.ReceiverId,
                    Sender = new ApplicationUser { FirstName = t.Sender.FirstName, LastName = t.Sender.LastName },
                    Receiver = new ApplicationUser { FirstName = t.Receiver.FirstName, LastName = t.Receiver.LastName }
                })
                .ToListAsync();

            return new SuccessDataResult<List<Transaction>>(transactions);
        }

        public async Task<IDataResult<List<Transaction>>> GetTransactionsByDateAsync(DateTime? startDate, DateTime? endDate)
        {

            var query = _uow.Repository<Transaction>()
                .Query()
                .Include(t => t.Sender)
                .Include(t => t.Receiver)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(t => t.CreatedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CreatedDate <= endDate.Value);

            var transactions = await query
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            return new SuccessDataResult<List<Transaction>>(transactions);


        }
        public async Task<IResult> AddAsync(Transaction entity)
        {
            await _uow.Repository<Transaction>().AddAsync(entity);
            await _uow.CommitAsync();

            return new SuccessResult(UIMessages.ADDED_MESSAGE);
        }

        public async Task<IResult> Delete(int id)
        {
            var transaction = await _uow.Repository<Transaction>().GetByIdAsync(id);

            if (transaction == null)
                return new ErrorResult("Transaction tapılmadı");

            _uow.Repository<Transaction>().Delete(transaction);
            await _uow.CommitAsync();

            return new SuccessResult(UIMessages.Deleted_MESSAGE);

        }

        public async Task<IDataResult<List<Transaction>>> GetAll()
        {
            var transactions = await _uow
               .Repository<Transaction>()
               .Query()
               .OrderByDescending(x => x.CreatedDate)
               .ToListAsync();

            return new SuccessDataResult<List<Transaction>>(transactions);
        }

        public async Task<IDataResult<Transaction>> GetById(int id)
        {
            var transaction = await _uow.Repository<Transaction>().GetByIdAsync(id);

            return new SuccessDataResult<Transaction>(transaction);
        }

        public async Task<IResult> Update(Transaction entity)
        {
            _uow.Repository<Transaction>().Update(entity);
            await _uow.CommitAsync();

            return new SuccessResult(UIMessages.UPDATE_MESSAGE);
        }



        public async Task<IDataResult<byte[]>> ExportTransactionsToPdfAsync(
            int userId,
            List<Transaction> transactions)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return new ErrorDataResult<byte[]>(null, "İstifadəçi tapılmadı");
                }

                using (var ms = new MemoryStream())
                {
                    var document = new Document(PageSize.A4, 25, 25, 30, 30);
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
                    return new SuccessDataResult<byte[]>(ms.ToArray(), "PDF yaradıldı");
                }
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<byte[]>(null,$"PDF yaradılmadı: {ex.Message}");
            }
        }

        public async Task<IDataResult<byte[]>> ExportTransactionsToExcelAsync(
            int userId,
            List<Transaction> transactions)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return new ErrorDataResult<byte[]>(null,"İstifadəçi tapılmadı");
                }

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Transactions");

                    // Headers
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
                        return new SuccessDataResult<byte[]>(stream.ToArray(), "Excel yaradıldı");
                    }
                }
            }
            catch (Exception ex)
            {
                return new ErrorDataResult<byte[]>(null,$"Excel yaradılmadı: {ex.Message}");
            }
        }
    }
}