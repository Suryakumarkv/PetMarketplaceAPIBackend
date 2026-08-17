using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetMarketplaceAPI.Models
{
    public class PetImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        public bool IsPrimary { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;

        [Required]
        public int PetId { get; set; }

        [ForeignKey("PetId")]
        public virtual Pet? Pet { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}