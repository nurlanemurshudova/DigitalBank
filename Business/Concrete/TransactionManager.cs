using Business.Abstract;
using Business.BaseMessages;
using Core.Results.Abstract;
using Core.Results.Concrete;
using DataAccess.UnitOfWork;
using Entities.Concrete.Dtos;
using Entities.Concrete.TableModels;
using Entities.Concrete.TableModels.Membership;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Concrete
{
    public class TransactionManager : ITransactionService
    {
        private readonly IUnitOfWork _uow;

        public TransactionManager(IUnitOfWork uow)
        {
            _uow = uow;
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
                        ReceiverId = receiver.Id,
                        ReceiverName = $"{receiver.FirstName} {receiver.LastName}",
                        NewBalance = receiver.Balance,
                        Amount = amount,
                        SenderName = $"{sender.FirstName} {sender.LastName}"
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
                .Include(t => t.Sender)
                .Include(t => t.Receiver)
                .Where(t => t.SenderId == userId || t.ReceiverId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            return new SuccessDataResult<List<Transaction>>(transactions);

        }


        public async Task<IDataResult<List<Transaction>>> GetAllTransactionsAsync()
        {

            var transactions = await _uow.Repository<Transaction>()
                .Query()
                .Include(t => t.Sender)
                .Include(t => t.Receiver)
                .OrderByDescending(t => t.CreatedDate)
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
    }
}