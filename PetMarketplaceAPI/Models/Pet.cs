using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetMarketplaceAPI.Models
{
    public class Pet
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Species { get; set; }

        [Required]
        public string Breed { get; set; }

        [Required]
        public int Age { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public string? Description { get; set; }

        public DateTime LastVaccinationDate { get; set; }
        public DateTime? NextVaccinationDate { get; set; }

        public string Status { get; set; } = "Available";

        public bool IsVaccinated { get; set; } = false;
        public string? MedicalHistory { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? LocationDescription { get; set; }

        public int SellerId { get; set; }
        public int? BuyerId { get; set; }

        public bool AdoptionConfirmed { get; set; } = false;
        public DateTime? AdoptionRequestDate { get; set; }
        public DateTime? AdoptionConfirmedDate { get; set; }

        public decimal? NegotiatedPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("SellerId")]
        public virtual User? Seller { get; set; }

        [ForeignKey("BuyerId")]
        public virtual User? Buyer { get; set; }

        public virtual ICollection<PetImage>? Images { get; set; }
        public virtual ICollection<VaccinationRecord>? Vaccinations { get; set; }
    }
}