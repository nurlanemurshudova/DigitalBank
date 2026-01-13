using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DigitalBankUI.Controllers
{
    [Authorize(Roles = "User")]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUnread()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _notificationService.GetUnreadNotificationsAsync(userId);

                if (result.IsSuccess)
                {
                    return Json(new
                    {
                        success = true,
                        notifications = result.Data.Select(n => new
                        {
                            id = n.Id,
                            message = n.Message,
                            createdDate = n.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                            isRead = n.IsRead
                        })
                    });
                }

                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var result = await _notificationService.MarkAsReadAsync(id);
                return Json(new { success = result.IsSuccess });
            }
            catch (Exception ex)
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _notificationService.MarkAllAsReadAsync(userId);
                return Json(new { success = result.IsSuccess });
            }
            catch (Exception ex)
            {
                return Json(new { success = false });
            }
        }
    }
}