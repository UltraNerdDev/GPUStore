using GPUStore.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GPUStore.ViewModels
{
    public class VideoCardCreateViewModel
    {
        // 1. Обектът, който ще запишем в базата
        public VideoCard VideoCard { get; set; } = new VideoCard();

        // 2. Списък за падащото меню (Dropdown) с производители
        // SelectList е специален тип в ASP.NET за работа с менюта
        public SelectList? Manufacturers { get; set; }

        // 3. Списък с технологии и информация дали са отметнати
        public List<TechnologySelection>? AvailableTechnologies { get; set; }
    }

    // Помощен клас за чекбоксовете
    public class TechnologySelection
    {
        public int TechnologyId { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }
}
