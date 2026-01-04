using Core.DataAccess.Abstract;
using Core.DataAccess.Concrete;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        //ITransactionDal TransactionDal { get; }
        //IMessageDal MessageDal { get; }
        //INotificationDal NotificationDal { get; }
        //Task<int> CommitAsync();

        //Task<int> SaveAsync();
        //Task BeginTransactionAsync();
        //Task CommitAsync();
        //Task RollbackAsync();
        IBaseRepository<T> Repository<T>() where T : class;
        Task<int> CommitAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackAsync();

    }
}
