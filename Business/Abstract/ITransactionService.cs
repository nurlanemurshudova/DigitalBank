using Core.Results.Abstract;
using Entities.Concrete.TableModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Abstract
{
    public interface ITransactionService
    {
        Task<IResult> TransferMoneyAsync(int senderId, string receiverAccountNumber, decimal amount, string description);

        Task<IDataResult<List<Transaction>>> GetUserTransactionsAsync(int userId);
        Task<IDataResult<List<Transaction>>> GetAllTransactionsAsync();
        Task<IDataResult<List<Transaction>>> GetTransactionsByDateAsync(DateTime? startDate, DateTime? endDate);



        Task<IResult> AddAsync(Transaction entity);
        Task<IResult>  Update(Transaction entity);
        Task<IResult> Delete(int id);
        Task<IDataResult<List<Transaction>>> GetAll();
        Task<IDataResult<Transaction>> GetById(int id);
    }
}
