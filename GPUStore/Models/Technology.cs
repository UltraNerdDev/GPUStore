using System.ComponentModel.DataAnnotations;

// ============================================================
// Models/Technology.cs — GPU технология (Ray Tracing, DLSS и др.)
// ============================================================

namespace GPUStore.Models
{
    public class Technology
    {
        [Key]
        public int Id { get; set; }

        // Наименование на технологията, напр. "Ray Tracing", "DLSS 3.0"
        [Required(ErrorMessage = "Името на технологията е задължително.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Минимум 2 символа.")]
        [Display(Name = "Технология")]
        public string Name { get; set; }

        // Навигация — всички карти, поддържащи тази технология
        public virtual ICollection<CardTechnology> CardTechnologies { get; set; } = new List<CardTechnology>();
    }
}
