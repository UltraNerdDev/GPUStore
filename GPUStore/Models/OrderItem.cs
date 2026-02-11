using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace GPUStore.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; } // Това също ще се инкрементира автоматично
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        public int VideoCardId { get; set; }
        [ForeignKey("VideoCardId")]
        public virtual VideoCard VideoCard { get; set; }

        public int Quantity { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal PriceAtPurchase { get; set; }
    }
}
