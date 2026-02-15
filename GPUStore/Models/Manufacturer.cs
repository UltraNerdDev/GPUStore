using System.ComponentModel.DataAnnotations;

// ============================================================
// Models/Manufacturer.cs — Производител на видеокарти
// ============================================================

namespace GPUStore.Models
{
    public class Manufacturer
    {
        [Key] 
        public int Id { get; set; }

        // Наименование на марката (2–50 символа), напр. "NVIDIA", "AMD", "ASUS"
        [Required(ErrorMessage = "Името на производителя е задължително.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Минимум 2 символа.")]
        [Display(Name = "Производител")]
        public string Name { get; set; }

        // Навигационна колекция — всички карти на този производител
        public virtual ICollection<VideoCard> VideoCards { get; set; } = new List<VideoCard>();
    }
}
