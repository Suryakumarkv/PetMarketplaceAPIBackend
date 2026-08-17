using System.ComponentModel.DataAnnotations;

namespace PetMarketplaceAPI.Models
{
    public class PetServiceProvider
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string ServiceType { get; set; } // Vet, Pharmacy, PetShop

        public string? Description { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string? GooglePlaceId { get; set; }
        public double Rating { get; set; } = 0;
        public int ReviewCount { get; set; } = 0;

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}