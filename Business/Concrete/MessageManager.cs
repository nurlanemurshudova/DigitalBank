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
    public class MessageManager : IMessageService
    {
        private readonly IUnitOfWork _uow;

        public MessageManager(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<IResult> SendMessageAsync(int senderId, int receiverId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new ErrorResult("Mesaj boş ola bilməz");

            if (senderId == receiverId)
                return new ErrorResult("Özünüzə mesaj göndərə bilməzsiniz");

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                IsRead = false
            };

            await _uow.Repository<Message>().AddAsync(message);
            await _uow.CommitAsync();

            return new SuccessResult("Mesaj göndərildi");
        }

        public async Task<IDataResult<List<Message>>> GetConversationAsync(int user1Id, int user2Id)
        {
            var messages = await _uow.Repository<Message>()
                .Query()
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                           (m.SenderId == user2Id && m.ReceiverId == user1Id))
                .OrderBy(m => m.CreatedDate)
                .ToListAsync();

            return new SuccessDataResult<List<Message>>(messages);
        }


        public async Task<IDataResult<int>> GetUnreadCountAsync(int userId)
        {
            var count = await _uow.Repository<Message>()
                .Query()
                .Where(m => m.ReceiverId == userId && !m.IsRead)
                .CountAsync();

            return new SuccessDataResult<int>(count);
        }

        public async Task<IResult> MarkAsReadAsync(int messageId)
        {
            var message = await _uow.Repository<Message>().GetByIdAsync(messageId);

            if (message == null)
                return new ErrorResult("Mesaj tapılmadı");

            message.IsRead = true;
            message.LastUpdatedDate = DateTime.Now;

            _uow.Repository<Message>().Update(message);
            await _uow.CommitAsync();

            return new SuccessResult("Mesaj oxundu");
        }

        public async Task<IResult> AddAsync(Message entity)
        {
            await _uow.Repository<Message>().AddAsync(entity);
            await _uow.CommitAsync();

            return new SuccessResult(UIMessages.ADDED_MESSAGE);
        }

        public async Task<IResult> Delete(int id)
        {
            var message = await _uow.Repository<Message>().GetByIdAsync(id);

            if (message == null)
                return new ErrorResult("Mesaj tapılmadı");

            _uow.Repository<Message>().Delete(message);
            await _uow.CommitAsync();

            return new SuccessResult(UIMessages.Deleted_MESSAGE);

        }

        public async Task<IDataResult<List<Message>>> GetAll()
        {
            var messages = await _uow
               .Repository<Message>()
               .Query()
               .OrderByDescending(x => x.CreatedDate)
               .ToListAsync();

            return new SuccessDataResult<List<Message>>(messages);
        }

        public async Task<IDataResult<Message>> GetById(int id)
        {
            var message = await _uow.Repository<Message>().GetByIdAsync(id);

            return new SuccessDataResult<Message>(message);
        }

        public async Task<IResult> Update(Message entity)
        {
            _uow.Repository<Message>().Update(entity);
            await _uow.CommitAsync();

            return new SuccessResult(UIMessages.UPDATE_MESSAGE);
        }
    }
}
