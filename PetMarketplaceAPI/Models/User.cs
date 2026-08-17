using System.ComponentModel.DataAnnotations;

namespace PetMarketplaceAPI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public string? PhoneNumber { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }

        public string Role { get; set; } = "Buyer";

        public string? ShopName { get; set; }
        public string? SellerDescription { get; set; }
        public bool IsVerifiedSeller { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Don't add ICollection navigation properties to avoid cascade issues
    }
}