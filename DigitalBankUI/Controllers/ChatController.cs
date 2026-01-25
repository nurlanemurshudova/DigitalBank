using Business.Abstract;
using Entities.Concrete.TableModels.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DigitalBankUI.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IMessageService _messageService;

        public ChatController(
            IMessageService messageService)
        {
            _messageService = messageService;
        }

        public async Task<IActionResult> Index()
        {

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdClaim)) return RedirectToAction("Login", "Account");

            int currentUserId = int.Parse(currentUserIdClaim);

            var result = await _messageService.GetAvailableUsersAsync(currentUserId);

            if (!result.IsSuccess)
            {
                TempData["Error"] = result.Message;
                return View(new List<ApplicationUser>());
            }

            ViewBag.CurrentUserId = currentUserId;
            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetConversation(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _messageService.GetConversationAsync(currentUserId, userId);

            return Json(result);
        }


        [HttpPost]
        public async Task<IActionResult> DeleteConversation(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _messageService.DeleteConversationAsync(currentUserId, userId);

            return Json(result);
        }
    }
}