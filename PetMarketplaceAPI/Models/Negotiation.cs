using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetMarketplaceAPI.Models
{
    public class Negotiation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PetId { get; set; }

        [Required]
        public int BuyerId { get; set; }

        [Required]
        public int SellerId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OfferedPrice { get; set; }

        public string? Message { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("PetId")]
        public virtual Pet? Pet { get; set; }

        [ForeignKey("BuyerId")]
        public virtual User? Buyer { get; set; }

        [ForeignKey("SellerId")]
        public virtual User? Seller { get; set; }
    }
}