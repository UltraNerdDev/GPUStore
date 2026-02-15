using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

// ============================================================
// Models/VideoCard.cs — Основен домейн модел: Видеокарта
// ============================================================

namespace GPUStore.Models
{
    public class VideoCard
    {
        // Първичен ключ — SQL Server го прави IDENTITY(auto-increment)
        [Key]
        public int Id { get; set; }

        // Наименование на модела (напр. "GeForce RTX 4090")
        // [Required] — не може да е null или празен; [StringLength] — между 2 и 100 символа
        [Required(ErrorMessage = "Името на модела е задължително.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Името трябва да е между 2 и 100 символа.")]
        [Display(Name = "Модел")]
        public string ModelName { get; set; }

        // Цена в лева — [Range] гарантира стойност между 0.01 и 20000
        // [Column(TypeName = "decimal(10,2)")] — DECIMAL(10,2) в SQL Server
        [Required(ErrorMessage = "Моля, въведете цена.")]
        [Range(0.01, 20000, ErrorMessage = "Цената трябва да е между 0.01 и 20 000 лв.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Цена")]
        public decimal Price { get; set; }

        // Чужд ключ (FK) към таблицата Manufacturers
        [Required(ErrorMessage = "Изберете производител.")]
        [Display(Name = "Производител")]
        public int ManufacturerId { get; set; }

        // Чужд ключ (FK) към таблицата Manufacturers
        [ForeignKey("ManufacturerId")]
        // Навигационно свойство — зарежда се при .Include(v => v.Manufacturer)
        public virtual Manufacturer Manufacturer { get; set; }

        // Id на администратора, добавил картата (незадължително)
        public string? AddedById { get; set; }

        // Само файловото наименование на снимката (напр. "4090.jpg")
        // Пълният path е wwwroot/images/{ImageUrl}
        [Display(Name = "URL на изображение")]
        public string? ImageUrl { get; set; }

        // Текстово описание за детайлната страница — до 2000 символа
        [Display(Name = "Описание")]
        [StringLength(2000, ErrorMessage = "Описанието не може да е над 2000 символа.")]
        public string? Description { get; set; }

        // Many-to-Many навигация към технологии чрез свързващата таблица CardTechnology
        // Инициализирана с празен List<> — защита срещу NullReferenceException
        public virtual ICollection<CardTechnology> CardTechnologies { get; set; } = new List<CardTechnology>();

        // Навигация към всички поръчки, в които е включена тази карта
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
