using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DigitalBankUI.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // User daxil olanda onun qrupuna əlavə et
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                //Console.WriteLine($"✅ User {userId} qoşuldu - ConnectionId: {Context.ConnectionId}");
            }

            await base.OnConnectedAsync();
        }

        // User çıxanda qrupdan sil
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
                //Console.WriteLine($"❌ User {userId} ayrıldı");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Transfer bildirişi göndər (istəyə bağlı - controller-dən də işləyir)
        public async Task SendTransferNotification(int receiverId, string message, decimal newBalance)
        {
            await Clients.Group($"user_{receiverId}").SendAsync("ReceiveNotification", new
            {
                message = message,
                newBalance = newBalance,
                timestamp = DateTime.Now
            });
        }
    }
}