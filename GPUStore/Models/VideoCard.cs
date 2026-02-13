using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GPUStore.Models
{
    public class VideoCard
    {
        [Key]
        public int Id { get; set; }


        [Required(ErrorMessage = "Името на модела е задължително.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Името трябва да е между 2 и 100 символа.")]
        [Display(Name = "Модел")]
        public string ModelName { get; set; }


        [Required(ErrorMessage = "Моля, въведете цена.")]
        [Range(0.01, 20000, ErrorMessage = "Цената трябва да е между 0.01 и 20 000 лв.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Цена")]
        public decimal Price { get; set; }


        [Required(ErrorMessage = "Изберете производител.")]
        [Display(Name = "Производител")]
        public int ManufacturerId { get; set; }


        [ForeignKey("ManufacturerId")]
        public virtual Manufacturer Manufacturer { get; set; }

        public string? AddedById { get; set; }


        [Display(Name = "URL на изображение")]
        public string? ImageUrl { get; set; }


        [Display(Name = "Описание")]
        [StringLength(2000, ErrorMessage = "Описанието не може да е над 2000 символа.")]
        public string? Description { get; set; } 

        public virtual ICollection<CardTechnology> CardTechnologies { get; set; } = new List<CardTechnology>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
