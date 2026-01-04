using Business.Abstract;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace DigitalBankUI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(IMessageService messageService, UserManager<ApplicationUser> userManager)
        {
            _messageService = messageService;
            _userManager = userManager;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(int receiverId, string message)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderIdStr))
                return;

            var senderId = int.Parse(senderIdStr);
            var sender = await _userManager.FindByIdAsync(senderId.ToString());

            var result = await _messageService.SendMessageAsync(senderId, receiverId, message);

            if (result.IsSuccess)
            {
                await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", new
                {
                    senderId = senderId,
                    senderName = $"{sender.FirstName} {sender.LastName}",
                    receiverId = receiverId,
                    message = message,
                    timestamp = DateTime.Now
                });

                await Clients.Caller.SendAsync("MessageSent", new
                {
                    success = true,
                    message = "Mesaj göndərildi"
                });
            }
        }

        public async Task UserTyping(int receiverId)
        {
            var senderIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderIdStr)) return;

            var senderId = int.Parse(senderIdStr);
            var sender = await _userManager.FindByIdAsync(senderId.ToString());

            await Clients.Group($"user_{receiverId}").SendAsync("UserIsTyping", new
            {
                userId = senderId,
                userName = $"{sender.FirstName} {sender.LastName}"
            });
        }

        public async Task MarkAsRead(int messageId)
        {
            await _messageService.MarkAsReadAsync(messageId);
        }
    }
}
