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
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(
            IMessageService messageService,
            UserManager<ApplicationUser> userManager)
        {
            _messageService = messageService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserIdClaim)) return RedirectToAction("Login", "Account");

            int currentUserId = int.Parse(currentUserIdClaim);

            var allUsersInUserRole = await _userManager.GetUsersInRoleAsync("User");

            var users = allUsersInUserRole
                .Where(u => u.Id != currentUserId)
                .OrderBy(u => u.FirstName)
                .ToList();

            ViewBag.CurrentUserId = currentUserId;
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetConversation(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _messageService.GetConversationAsync(currentUserId, userId);

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var result = await _messageService.GetUnreadCountAsync(currentUserId);

            return Json(result);
        }

    }
}