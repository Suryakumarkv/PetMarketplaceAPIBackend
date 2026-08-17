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
    public class NegotiationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NegotiationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/Negotiations - Create negotiation AND chat message
        [HttpPost]
        public async Task<ActionResult> CreateNegotiation([FromBody] NegotiationRequest request)
        {
            var buyerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var pet = await _context.Pets
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == request.PetId);

            if (pet == null) return NotFound(new { message = "Pet not found" });

            // Create negotiation
            var negotiation = new Negotiation
            {
                PetId = request.PetId,
                BuyerId = buyerId,
                SellerId = pet.SellerId,
                OfferedPrice = request.OfferedPrice,
                Message = request.Message,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Negotiations.Add(negotiation);

            // Create chat message to seller
            var chatContent = $"Hi, I'm interested in {pet.Name}. My offer is ₹{request.OfferedPrice}.";
            if (!string.IsNullOrEmpty(request.Message))
            {
                chatContent += $" {request.Message}";
            }

            var chatMessage = new ChatMessage
            {
                SenderId = buyerId,
                ReceiverId = pet.SellerId,
                PetId = request.PetId,
                Content = chatContent,
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chatMessage);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Offer sent successfully",
                negotiationId = negotiation.Id,
                chatMessageId = chatMessage.Id
            });
        }

        // GET: api/Negotiations/my
        [HttpGet("my")]
        public async Task<ActionResult> GetMyNegotiations()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var negotiations = await _context.Negotiations
                .Include(n => n.Pet)
                .Include(n => n.Buyer)
                .Include(n => n.Seller)
                .Where(n => n.BuyerId == userId || n.SellerId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(negotiations);
        }
    }

    public class NegotiationRequest
    {
        public int PetId { get; set; }
        public decimal OfferedPrice { get; set; }
        public string? Message { get; set; }
    }
}