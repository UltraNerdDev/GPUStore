// ============================================================
// ViewModels/VideoCardCreateViewModel.cs
// ============================================================
// ViewModel за формата за СЪЗДАВАНЕ и РЕДАКТИРАНЕ на видеокарти.
//
// Защо ViewModel вместо директен VideoCard модел?
// Формата се нуждае от данни, НЕВКЛЮЧЕНИ в VideoCard модела:
//   - Списък с производители за <select> dropdown
//   - Списък с технологии + флаг "избрана ли е" за <input type="checkbox">
//   - IFormFile за качване на изображение
// ============================================================

using GPUStore.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GPUStore.ViewModels
{
    public class VideoCardCreateViewModel
    {
        // 1. Основният VideoCard обект, който ще бъде записан в базата.
        //    Инициализиран с new VideoCard() — избягва NullReferenceException в изгледа.
        public VideoCard VideoCard { get; set; } = new VideoCard();

        // 2. SelectList за HTML <select> dropdown с производители.
        //    SelectList(items, valueField, textField) — ASP.NET Core Tag Helper
        //    автоматично генерира <option value="{Id}">{Name}</option> за всеки елемент.
        //    nullable — не е задължителен при POST (не се изпраща обратно от формата)
        public SelectList? Manufacturers { get; set; }

        // 3. Списък с всички технологии + дали всяка е отметната.
        //    При GET (Create): IsSelected = false за всички
        //    При GET (Edit): IsSelected = true само за свързаните с картата
        //    При POST: съдържа изборите на потребителя
        public List<TechnologySelection>? AvailableTechnologies { get; set; }

        // 4. Качения файл от <input type="file">.
        //    IFormFile е специален тип за HTTP multipart form-data качвания.
        //    nullable — не е задължително да се качва нова снимка при редактиране.
        public IFormFile? ImageFile { get; set; }
    }

    // ── Помощен клас за чекбокс ──
    // Използва се в цикъл за рендиране на чекбокс за всяка технология.
    // Трябва да е отделен клас (не вграден анонимен тип),
    // защото ASP.NET Core Model Binding изисква конкретен тип за списъчно binding.
    public class TechnologySelection
    {
        // Id на технологията — скрит input в чекбокса (за Model Binding)
        public int TechnologyId { get; set; }

        // Наименование за показване на label
        public string Name { get; set; }

        // Флаг: checked="checked" при IsSelected = true
        public bool IsSelected { get; set; }
    }
}