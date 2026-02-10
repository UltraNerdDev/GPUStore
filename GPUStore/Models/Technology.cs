using System.ComponentModel.DataAnnotations;

namespace GPUStore.Models
{
    public class Technology
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public virtual ICollection<CardTechnology> CardTechnologies { get; set; } = new List<CardTechnology>();
    }
}
