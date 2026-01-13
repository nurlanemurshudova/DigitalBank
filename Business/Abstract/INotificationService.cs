using Core.Results.Abstract;
using Entities.Concrete.TableModels;

namespace Business.Abstract
{
    public interface INotificationService
    {

        Task<IDataResult<List<Notification>>> GetUnreadNotificationsAsync(int userId);
        Task<IResult> MarkAsReadAsync(int notificationId);
        Task<IResult> MarkAllAsReadAsync(int userId);
        Task<IResult> AddAsync(Notification entity);
        Task<IResult> Update(Notification entity);
        Task<IResult> Delete(int id);
        Task<IDataResult<List<Notification>>> GetAll();
        Task<IDataResult<Notification>> GetById(int id);
    }
}
