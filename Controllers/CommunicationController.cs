using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.ViewModels.Communication;

namespace Staybnb.Controllers
{
    [Authorize]
    public class CommunicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommunicationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==========================================
        // 1. MESSAGING INBOX & CONVERSATIONS
        // ==========================================

        // GET: /Communication/Inbox
        public async Task<IActionResult> Inbox()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            // Retrieve all messages involving current user
            var allMessages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            // Group by the other user
            var conversationGroups = allMessages
                .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .ToList();

            var conversationSummaries = new List<ConversationSummaryDto>();

            foreach (var group in conversationGroups)
            {
                var otherUserId = group.Key;
                var latestMessage = group.First();
                var otherUser = latestMessage.SenderId == currentUserId ? latestMessage.Receiver : latestMessage.Sender;

                if (otherUser != null)
                {
                    conversationSummaries.Add(new ConversationSummaryDto
                    {
                        OtherUserId = otherUserId,
                        OtherUserName = otherUser.FullName,
                        OtherUserProfilePicture = otherUser.ProfilePictureUrl,
                        LastMessageContent = latestMessage.Content,
                        LastMessageTime = latestMessage.Timestamp,
                        IsOutgoing = latestMessage.SenderId == currentUserId
                    });
                }
            }

            var model = new InboxViewModel
            {
                Conversations = conversationSummaries
            };

            return View(model);
        }

        // GET: /Communication/Conversation?userId=xyz
        public async Task<IActionResult> Conversation(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            var otherUser = await _userManager.FindByIdAsync(userId);
            if (otherUser == null)
            {
                return NotFound("Conversation participant not found.");
            }

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                             (m.SenderId == userId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            var model = new ConversationViewModel
            {
                OtherUserId = otherUser.Id,
                OtherUserName = otherUser.FullName,
                OtherUserProfilePicture = otherUser.ProfilePictureUrl,
                Messages = messages
            };

            return View(model);
        }

        // POST: /Communication/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(string receiverId, string content)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction(nameof(Conversation), new { userId = receiverId });
            }

            var message = new Message
            {
                SenderId = currentUserId,
                ReceiverId = receiverId,
                Content = content.Trim(),
                Timestamp = DateTime.UtcNow
            };

            _context.Messages.Add(message);

            // Pure database-backed notification for the receiver
            var sender = await _userManager.FindByIdAsync(currentUserId);
            _context.Notifications.Add(new Notification
            {
                UserId = receiverId,
                Title = $"New message from {sender?.FullName ?? "a user"}",
                Type = "Message",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Conversation), new { userId = receiverId });
        }

        // ==========================================
        // 2. NOTIFICATIONS CENTER
        // ==========================================

        // GET: /Communication/Notifications
        public async Task<IActionResult> Notifications()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Challenge();
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUserId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(notifications);
        }

        // POST: /Communication/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUserId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Notifications));
        }

        // POST: /Communication/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUserId && !n.IsRead)
                .ToListAsync();

            foreach (var item in notifications)
            {
                item.IsRead = true;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All notifications marked as read.";
            return RedirectToAction(nameof(Notifications));
        }
    }
}
