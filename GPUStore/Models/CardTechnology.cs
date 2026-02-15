// ============================================================
// Models/CardTechnology.cs — Свързваща таблица Many-to-Many
//                            между VideoCard и Technology
// ============================================================
// Тази таблица реализира релацията "много-към-много":
// - Една видеокарта може да поддържа много технологии
// - Една технология може да се поддържа от много карти
//
// Съставният ключ (VideoCardId + TechnologyId) се конфигурира
// в ApplicationDbContext.OnModelCreating() с Fluent API.
// ============================================================

namespace GPUStore.Models
{
    public class CardTechnology
    {
        // Чужд ключ към VideoCards — ПОЛОВИНАТА на съставния PK
        public int VideoCardId { get; set; }
        public virtual VideoCard VideoCard { get; set; }

        // Чужд ключ към Technologies — ДРУГАТА половина на съставния PK
        public int TechnologyId { get; set; }
        public virtual Technology Technology { get; set; }
    }
}
