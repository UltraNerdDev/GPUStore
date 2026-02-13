using System.ComponentModel.DataAnnotations;

namespace GPUStore.Models
{
    public class Manufacturer
    {
        [Key] 
        public int Id { get; set; }

        [Required(ErrorMessage = "Името на производителя е задължително.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Минимум 2 символа.")]
        [Display(Name = "Производител")]
        public string Name { get; set; }
        public virtual ICollection<VideoCard> VideoCards { get; set; } = new List<VideoCard>();
    }
}
