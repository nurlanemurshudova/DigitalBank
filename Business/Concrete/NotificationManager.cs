using Business.Abstract;
using Business.BaseMessages;
using Core.Results.Abstract;
using Core.Results.Concrete;
using DataAccess.UnitOfWork;
using Entities.Concrete.TableModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Concrete
{
    public class NotificationManager : INotificationService
    {
        private readonly IUnitOfWork _uow;

        public NotificationManager(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<IDataResult<List<Notification>>> GetUnreadNotificationsAsync(int userId)
        {
            var notifications = await _uow.Repository<Notification>()
                .Query()
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return new SuccessDataResult<List<Notification>>(notifications);
        }

        public async Task<IResult> MarkAsReadAsync(int notificationId)
        {
            var notification = await _uow.Repository<Notification>().GetByIdAsync(notificationId);

            if (notification == null)
                return new ErrorResult("Bildiriş tapılmadı");

            notification.IsRead = true;
            _uow.Repository<Notification>().Update(notification);
            await _uow.CommitAsync();

            return new SuccessResult("Bildiriş oxundu");
        }

        public async Task<IResult> MarkAllAsReadAsync(int userId)
        {
            var notifications = await _uow.Repository<Notification>()
                .Query()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                _uow.Repository<Notification>().Update(notification);
            }

            await _uow.CommitAsync();
            return new SuccessResult("Bütün bildirişlər oxundu");
        }
    
        public async Task<IResult> AddAsync(Notification entity)
        {
            await _uow.Repository<Notification>().AddAsync(entity);
            await _uow.CommitAsync();

            return new SuccessResult(UIMessages.ADDED_MESSAGE);
        }

        public async Task<IResult> Delete(int id)
        {
            var notification = await _uow.Repository<Notification>().GetByIdAsync(id);

            if (notification == null)
                return new ErrorResult("Bildiris tapılmadı");

            _uow.Repository<Notification>().Delete(notification);
            await _uow.CommitAsync();

            return new SuccessResult(UIMessages.Deleted_MESSAGE);

        }

        public async Task<IDataResult<List<Notification>>> GetAll()
        {
            var notifications = await _uow
               .Repository<Notification>()
               .Query()
               .OrderByDescending(x => x.CreatedDate)
               .ToListAsync();

            return new SuccessDataResult<List<Notification>>(notifications);
        }

        public async Task<IDataResult<Notification>> GetById(int id)
        {
            var notification = await _uow.Repository<Notification>().GetByIdAsync(id);

            return new SuccessDataResult<Notification>(notification);
        }

        public async Task<IResult> Update(Notification entity)
        {
            _uow.Repository<Notification>().Update(entity);
            await _uow.CommitAsync();

            return new SuccessResult(UIMessages.UPDATE_MESSAGE);
        }
    }
}
