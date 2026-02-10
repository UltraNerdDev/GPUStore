using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GPUStore.Models
{
    public class VideoCard
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string ModelName { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public int ManufacturerId { get; set; }
        [ForeignKey("ManufacturerId")]
        public virtual Manufacturer Manufacturer { get; set; }

        public string? AddedById { get; set; }

        public virtual ICollection<CardTechnology> CardTechnologies { get; set; } = new List<CardTechnology>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
