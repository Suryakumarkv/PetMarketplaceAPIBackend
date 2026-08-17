using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetMarketplaceAPI.Data;
using PetMarketplaceAPI.Models;
using System.Security.Claims;

namespace PetMarketplaceAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Chat/conversations - Grouped by user AND pet
        [HttpGet("conversations")]
        public async Task<ActionResult> GetConversations()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Pet)
                    .ThenInclude(p => p.Images)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => new
                {
                    OtherUserId = m.SenderId == userId ? m.ReceiverId : m.SenderId,
                    PetId = m.PetId ?? 0
                })
                .Select(g => new
                {
                    OtherUserId = g.Key.OtherUserId,
                    PetId = g.Key.PetId == 0 ? (int?)null : g.Key.PetId,
                    OtherUserName = g.First().SenderId == userId
                        ? $"{g.First().Receiver.FirstName} {g.First().Receiver.LastName}"
                        : $"{g.First().Sender.FirstName} {g.First().Sender.LastName}",
                    OtherUserRole = g.First().SenderId == userId
                        ? g.First().Receiver.Role
                        : g.First().Sender.Role,
                    LastMessage = g.OrderByDescending(m => m.SentAt).First().Content,
                    LastMessageTime = g.OrderByDescending(m => m.SentAt).First().SentAt,
                    LastMessageSenderId = g.OrderByDescending(m => m.SentAt).First().SenderId,
                    UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead),
                    PetName = g.First().Pet?.Name,
                    PetImage = g.First().Pet?.Images?.FirstOrDefault()?.ImageUrl,
                    PetStatus = g.First().Pet?.Status,
                    PetBuyerId = g.First().Pet?.BuyerId,
                    PetSellerId = g.First().Pet?.SellerId,
                    ConversationKey = $"{g.Key.OtherUserId}_{g.Key.PetId}"
                })
                .OrderByDescending(c => c.LastMessageTime)
                .ToList();

            return Ok(conversations);
        }

        // GET: api/Chat/messages/5/1 - Get messages with user for specific pet
        [HttpGet("messages/{otherUserId}/{petId?}")]
        public async Task<ActionResult> GetMessages(int otherUserId, int? petId = null)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var query = _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Pet)
                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                           (m.SenderId == otherUserId && m.ReceiverId == userId));

            if (petId.HasValue)
            {
                query = query.Where(m => m.PetId == petId.Value);
            }
            else
            {
                query = query.Where(m => m.PetId == null);
            }

            var messages = await query.OrderBy(m => m.SentAt).ToListAsync();

            var unreadMessages = messages.Where(m => m.ReceiverId == userId && !m.IsRead);
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();

            var result = messages.Select(m => new
            {
                m.Id,
                m.Content,
                m.SenderId,
                m.ReceiverId,
                m.PetId,
                PetName = m.Pet?.Name,
                m.IsRead,
                m.SentAt,
                SenderName = $"{m.Sender.FirstName} {m.Sender.LastName}"
            });

            return Ok(result);
        }

        // POST: api/Chat/send - Send message
        [HttpPost("send")]
        public async Task<ActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                PetId = request.PetId,
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Message sent",
                chatMessageId = message.Id
            });
        }

        // GET: api/Chat/unread-count
        [HttpGet("unread-count")]
        public async Task<ActionResult> GetUnreadCount()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var count = await _context.ChatMessages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);

            return Ok(new { unreadCount = count });
        }
    }

    public class SendMessageRequest
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; }
        public int? PetId { get; set; }
    }
}