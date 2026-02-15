using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

// ============================================================
// Models/OrderItem.cs — Елемент от поръчка (конкретна карта)
// ============================================================
// OrderItem свързва Order с VideoCard и пази историческите данни:
// - Quantity: колко бройки са поръчани
// - PriceAtPurchase: ЦЕНАТА В МОМЕНТА НА ПОКУПКАТА
//
// PriceAtPurchase е критично важно — ако Admin промени цената на
// картата след поръчката, тя трябва да остане със старата цена.
// ============================================================

namespace GPUStore.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        // Към коя поръчка принадлежи този елемент
        public int OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        // Коя видеокарта е поръчана
        public int VideoCardId { get; set; }
        [ForeignKey("VideoCardId")]
        public virtual VideoCard VideoCard { get; set; }

        // Колко бройки са поръчани
        public int Quantity { get; set; }

        // ИСТОРИЧЕСКА ЦЕНА — зафиксирана в момента на покупката.
        [Column(TypeName = "decimal(10,2)")]
        public decimal PriceAtPurchase { get; set; }
    }
}
