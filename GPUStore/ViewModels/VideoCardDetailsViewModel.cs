// ============================================================
// ViewModels/VideoCardDetailsViewModel.cs
// ============================================================
// ViewModel за детайлната страница на видеокарта (UserDetails).
// Обединява информацията за картата с нейните коментари.
// ============================================================

using GPUStore.Models;
namespace GPUStore.ViewModels
{
    public class VideoCardDetailsViewModel
    {
        // Пълният VideoCard обект с Manufacturer и CardTechnologies
        public VideoCard VideoCard { get; set; }

        // Всички коментари за тази карта, сортирани от най-нови към най-стари
        public IEnumerable<Comment> Comments { get; set; }

        // Поле за нов коментар от формата (за двупосочно binding)
        // Не се използва за записване директно — контролерът чете content от query string
        public string NewCommentContent { get; set; }
    }
}