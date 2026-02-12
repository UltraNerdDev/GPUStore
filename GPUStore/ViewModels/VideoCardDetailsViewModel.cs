using GPUStore.Models;

namespace GPUStore.ViewModels
{
    public class VideoCardDetailsViewModel
    {
        public VideoCard VideoCard { get; set; }
        public IEnumerable<Comment> Comments { get; set; }
        public string NewCommentContent { get; set; } // За формата за нов коментар
    }
}
