using Core.Results.Abstract;
using Entities.Concrete.Dtos;
using Entities.Concrete.TableModels;

namespace Business.Abstract
{
    public interface INotificationService
    {

        Task<IDataResult<List<Notification>>> GetUnreadNotificationsAsync(int userId);
        Task<IResult> MarkAsReadAsync(int notificationId);
        Task<IResult> MarkAllAsReadAsync(int userId);
    }
}
