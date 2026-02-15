using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

// ============================================================
// Models/CartItem.cs — Продукт в активна пазарска количка
// ============================================================
// CartItem е ВРЕМЕНЕН запис — съществува само докато потребителят
// не завърши поръчката. При ConfirmOrder всички CartItems на
// потребителя се изтриват и се създава Order с OrderItems.
// ============================================================

namespace GPUStore.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        // Id на потребителя, чиято количка е (от AspNetUsers таблицата на Identity)
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        // Коя видеокарта е добавена
        public int VideoCardId { get; set; }
        [ForeignKey("VideoCardId")]
        public virtual VideoCard VideoCard { get; set; }

        // Брой бройки — по подразбиране 1, може да се увеличава чрез +/- бутоните
        public int Quantity { get; set; } = 1;
    }
}
