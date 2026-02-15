// ============================================================
// Data/ApplicationDbContext.cs — Контекст на базата данни
// ============================================================
// ApplicationDbContext е централният клас за работа с базата данни.
// Той наследява IdentityDbContext, което автоматично добавя всички
// Identity таблици: AspNetUsers, AspNetRoles, AspNetUserRoles и др.
//
// EF Core използва DbSet свойствата, за да знае кои таблици да управлява.
// Чрез OnModelCreating настройваме релациите, ключовете и ограниченията.
// ============================================================

using GPUStore.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Data
{
    // IdentityDbContext вместо базовия DbContext — включва Identity таблиците автоматично.
    // Например: AspNetUsers (потребители), AspNetRoles (роли), AspNetUserRoles (връзка роли-потребители) и др.
    public class ApplicationDbContext : IdentityDbContext
    {
        // Конструкторът приема DbContextOptions и ги предава на базовия клас.
        // DI контейнерът на ASP.NET Core автоматично инжектира options с
        // connection string-а и SQL Server провайдъра (конфигурирани в Program.cs).
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ────────────────────────────────────────────────────────
        // DbSet свойства — всяко свойство = таблица в базата данни
        // ────────────────────────────────────────────────────────

        // Таблица с видеокарти — централният продуктов каталог
        public DbSet<VideoCard> VideoCards { get; set; }

        // Таблица с производители (NVIDIA, AMD, ASUS и др.)
        public DbSet<Manufacturer> Manufacturers { get; set; }

        // Справочна таблица с GPU технологии (Ray Tracing, DLSS и др.)
        public DbSet<Technology> Technologies { get; set; }

        // Свързваща таблица Many-to-Many между VideoCards и Technologies.
        // Тъй като нямаме допълнителни данни в нея, EF Core може да я управлява
        // автоматично, но я декларираме изрично за по-голям контрол.
        public DbSet<CardTechnology> CardTechnologies { get; set; }

        // Таблица с поръчки — всяка завършена покупка
        public DbSet<Order> Orders { get; set; }

        // Таблица с елементи от поръчки (кои карти са в коя поръчка, с цена при покупка)
        public DbSet<OrderItem> OrderItems { get; set; }

        // Таблица с активни колички — временни записи, докато потребителят пазарува
        public DbSet<CartItem> CartItems { get; set; }

        // Таблица с коментари към видеокарти
        public DbSet<Comment> Comments { get; set; }

        // ────────────────────────────────────────────────────────
        // OnModelCreating — Fluent API конфигурация на схемата
        // ────────────────────────────────────────────────────────
        // Извиква се при първото изграждане на модела (веднъж при стартиране).
        // Тук дефинираме нещата, които не могат да се направят само с атрибути:
        // съставни ключове, специфични релации, ограничения и т.н.
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // ЗАДЪЛЖИТЕЛНО: извикваме базовия метод, за да се конфигурират
            // всички Identity таблици (AspNetUsers, AspNetRoles и т.н.).
            // Ако пропуснем това — Identity не работи!
            base.OnModelCreating(builder);

            // ── Конфигурация 1: CardTechnology (Many-to-Many: VideoCard ↔ Technology) ──
            // CardTechnology не е стандартна таблица с автоинкрементен Id.
            // Вместо това използва СЪСТАВЕН ПЪРВИЧЕН КЛЮЧ (composite PK)
            // от двете чужди ключове: VideoCardId + TechnologyId.
            // Това гарантира, че същата технология не може да се добави
            // два пъти към една и съща видеокарта.
            builder.Entity<CardTechnology>()
                .HasKey(ct => new { ct.VideoCardId, ct.TechnologyId });

            // ── Конфигурация 2: OrderItem релации ──
            // OrderItem е свързваща таблица между Order и VideoCard,
            // но има допълнителни данни (Quantity, PriceAtPurchase),
            // затова се конфигурира явно с HasOne/WithMany.

            // Релация: OrderItem → Order (много поръчки могат да имат много елементи)
            // Един OrderItem принадлежи на точно един Order.
            // Един Order може да съдържа много OrderItems.
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)        // OrderItem има един Order
                .WithMany(o => o.OrderItems)   // Order има много OrderItems
                .HasForeignKey(oi => oi.OrderId); // Чуждият ключ е OrderId

            // Релация: OrderItem → VideoCard
            // Един OrderItem се отнася за точно една VideoCard.
            // Една VideoCard може да се появява в много OrderItems (в различни поръчки).
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.VideoCard)        // OrderItem има една VideoCard
                .WithMany(v => v.OrderItems)        // VideoCard има много OrderItems
                .HasForeignKey(oi => oi.VideoCardId); // Чуждият ключ е VideoCardId
        }
    }
}
