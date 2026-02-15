using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

// ============================================================
// Models/Order.cs — Завършена поръчка
// ============================================================
// Order се създава при ConfirmOrder. Съдържа обобщена информация:
// кой е поръчал, кога, на каква обща сума и текущия статус.
// Детайлите (кои конкретни карти) са в OrderItems.
// ============================================================

namespace GPUStore.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // Дата и час на поръчката — автоматично се задава при създаване
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Id на потребителя, направил поръчката (FK към AspNetUsers)
        public string UserId { get; set; }
        [Column(TypeName = "decimal(10,2)")]

        // ДОБАВИ ТЕЗИ РЕДОВЕ:
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        // Обща сума — изчислена при ConfirmOrder като сума от (Quantity * Price) за всеки продукт
        // Историческа стойност: не се променя ако цените на продуктите се обновят!

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        // Статус на поръчката — управляван от Admin чрез Details страницата.
        // Възможни стойности: "Pending" (изчакваща), "Processed" (обработена),
        //                     "Shipped" (изпратена), "Cancelled" (отказана)
        public string Status { get; set; } = "Изчакваща";

        // Навигационна колекция към конкретните продукти в поръчката
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
