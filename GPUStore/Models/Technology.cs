using System.ComponentModel.DataAnnotations;

namespace GPUStore.Models
{
    public class Technology
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Името на технологията е задължително.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Минимум 2 символа.")]
        [Display(Name = "Технология")]
        public string Name { get; set; }
        public virtual ICollection<CardTechnology> CardTechnologies { get; set; } = new List<CardTechnology>();
    }
}
