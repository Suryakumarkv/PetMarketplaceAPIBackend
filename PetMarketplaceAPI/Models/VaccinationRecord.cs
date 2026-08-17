using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetMarketplaceAPI.Models
{
    public class VaccinationRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string VaccineName { get; set; }

        [Required]
        public DateTime VaccinationDate { get; set; }

        public DateTime? NextDueDate { get; set; }

        public string? VeterinarianName { get; set; }
        public string? ClinicName { get; set; }
        public string? Notes { get; set; }

        [Required]
        public int PetId { get; set; }

        [ForeignKey("PetId")]
        public virtual Pet? Pet { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}