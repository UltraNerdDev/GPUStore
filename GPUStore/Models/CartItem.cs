using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GPUStore.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        public int VideoCardId { get; set; }
        [ForeignKey("VideoCardId")]
        public virtual VideoCard VideoCard { get; set; }

        public int Quantity { get; set; } = 1;
    }
}
