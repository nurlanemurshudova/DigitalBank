using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DigitalBankApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("getall")]
        public async Task<IActionResult> GetNotifications()
        {
            var result = await _notificationService.GetAll();
            if (!result.IsSuccess)
                return BadRequest(new { success = false, message = "Xəta baş verdi" });

            // BURADA WHERE ŞƏRTİNİ SİLDİK:
            var notifications = result.Data.OrderByDescending(n => n.CreatedDate);

            return Ok(new
            {
                success = true,
                count = notifications.Count(),
                unreadCount = notifications.Count(n => !n.IsRead),
                notifications = notifications.Select(n => new {
                    n.Id,
                    n.UserId, // Kimə aid olduğunu görmək üçün UserId-ni də əlavə etdik
                    n.Message,
                    n.IsRead,
                    n.CreatedDate
                })
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _notificationService.GetById(id);

            if (result.Data == null)
                return NotFound(new { success = false, message = "Bildiriş tapılmadı" });

            // Qeyd: Əgər istənilən userin bildirişini görmək istəyirsinizsə, 
            // aşağıdakı kimi birbaşa datanı qaytarın:
            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await _notificationService.GetById(id);
            if (notification.Data == null)
                return NotFound(new { success = false, message = "Bildiriş tapılmadı" });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            if (notification.Data.UserId != userId)
                return Forbid();

            var result = await _notificationService.Delete(id);
            if (result.IsSuccess)
                return Ok(new { success = true, message = "Bildiriş silindi" });

            return BadRequest(new { success = false, message = "Xəta baş verdi" });
        }
    }
}
