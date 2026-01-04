using Core.Results.Abstract;
using Entities.Concrete.TableModels;

namespace Business.Abstract
{
    public interface IMessageService
    {
        Task<IResult> SendMessageAsync(int senderId, int receiverId, string content);
        Task<IDataResult<List<Message>>> GetConversationAsync(int user1Id, int user2Id);
        Task<IDataResult<int>> GetUnreadCountAsync(int userId);
        Task<IResult> MarkAsReadAsync(int messageId);

        Task<IResult> AddAsync(Message entity);
        Task<IResult> Update(Message entity);
        Task<IResult> Delete(int id);
        Task<IDataResult<List<Message>>> GetAll();
        Task<IDataResult<Message>> GetById(int id);
    }
}
