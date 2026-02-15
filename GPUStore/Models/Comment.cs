using Microsoft.AspNetCore.Identity;

// ============================================================
// Models/Comment.cs — Коментар към видеокарта
// ============================================================
// Коментарите могат да се добавят само от клиенти (не Admin).
// Показват се на детайлната страница на всяка видеокарта,
// сортирани от най-нов към най-стар.
// ============================================================

namespace GPUStore.Models
{
    public class Comment
    {
        public int Id { get; set; }

        // Текст на коментара
        public string Content { get; set; }

        // Дата и час на публикуване — автоматично при добавяне
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // За коя видеокарта е коментарът
        public int VideoCardId { get; set; }
        public VideoCard VideoCard { get; set; }

        // Кой потребител е написал коментара (FK към AspNetUsers)
        public string UserId { get; set; } // Кой е написал коментара
        public IdentityUser User { get; set; }
    }
}
