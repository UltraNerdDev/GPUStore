using Microsoft.AspNetCore.Identity;

namespace GPUStore.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int VideoCardId { get; set; }
        public VideoCard VideoCard { get; set; }

        public string UserId { get; set; } // Кой е написал коментара
        public IdentityUser User { get; set; }
    }
}
