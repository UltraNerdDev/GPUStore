using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GPUStore.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public string UserId { get; set; }
        [Column(TypeName = "decimal(10,2)")]

        // ДОБАВИ ТЕЗИ РЕДОВЕ:
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        // Добавяме и статус, за да може админът да го променя
        public string Status { get; set; } = "Pending";
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
