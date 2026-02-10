using System.ComponentModel.DataAnnotations;

namespace GPUStore.Models
{
    public class Manufacturer
    {
        [Key] 
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public virtual ICollection<VideoCard> VideoCards { get; set; } = new List<VideoCard>();
    }
}
