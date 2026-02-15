// ============================================================
// Controllers/VideoCardsController.cs — Управление на видеокарти
// ============================================================
// Контролерът е разделен на два "слоя":
//   1. Admin функции (CRUD): Index, Create, Edit, Delete
//   2. Клиентски функции: UserIndex (каталог), UserDetails, AddComment
//
// [Authorize] на ниво клас = всички методи изискват вход.
// Допълнително [Authorize(Roles = "Admin")] на CRUD методите.
// ============================================================

using GPUStore.Data;
using GPUStore.Models;
using GPUStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GPUStore.Controllers
{
    // [Authorize] — всеки, дори анонимен потребител, се пренасочва към Login
    [Authorize]
    public class VideoCardsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // IWebHostEnvironment дава достъп до пътищата на файловата система,
        // по-специално WebRootPath (= пътят до wwwroot/).
        // Нужен за запис на качените изображения.
        private readonly IWebHostEnvironment _webHostEnvironment;

        public VideoCardsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ═══════════════════════════════════════════════════════
        // ADMIN ФУНКЦИИ
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// GET: /VideoCards  (Admin изглед)
        /// Показва таблица с ВСИЧКИ видеокарти за административно управление.
        /// Include() зарежда навигационните свойства (Manufacturer, Technologies)
        /// в ЕДИН SQL JOIN, вместо N+1 заявки.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var cards = await _context.VideoCards
                .Include(v => v.Manufacturer)          // JOIN към Manufacturers таблицата
                .Include(v => v.CardTechnologies)       // JOIN към CardTechnologies
                    .ThenInclude(ct => ct.Technology)   // и оттам към Technologies
                .ToListAsync();
            return View(cards);
        }

        /// <summary>
        /// GET: /VideoCards/Create  (Admin изглед)
        /// Зарежда празна форма за добавяне на нова видеокарта.
        /// Подготвя:
        ///   - SelectList за падащото меню с производители
        ///   - List&lt;TechnologySelection&gt; за чекбоксовете с технологии
        /// </summary>
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            var viewModel = new VideoCardCreateViewModel
            {
                // SelectList(source, valueField, textField) — генерира HTML <option> елементи.
                // "Id" = стойността, "Name" = видимия текст
                Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name"),

                // За всяка технология в базата създаваме TechnologySelection обект.
                // IsSelected = false — нито една не е избрана по подразбиране.
                AvailableTechnologies = _context.Technologies.Select(t => new TechnologySelection
                {
                    TechnologyId = t.Id,
                    Name = t.Name,
                    IsSelected = false
                }).ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// POST: /VideoCards/Create
        /// Обработва изпратената форма за нова видеокарта.
        /// Стъпки:
        ///   1. Проверка за дубликат (ModelName + ManufacturerId)
        ///   2. Качване на снимката (ако е подадена)
        ///   3. Запис на VideoCard в базата
        ///   4. Запис на избраните технологии (CardTechnology записи)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VideoCardCreateViewModel model)
        {
            // Навигационните свойства (Manufacturer, CardTechnologies) не се изпращат от формата.
            // Ако не ги премахнем от ModelState — валидацията ще смята, че трябва да са попълнени.
            ModelState.Remove("VideoCard.Manufacturer");
            ModelState.Remove("VideoCard.CardTechnologies");

            // СТЪПКА 1: Проверка за дублиращ запис ПРЕДИ ModelState.IsValid.
            // Комбинацията ModelName + ManufacturerId трябва да е уникална.
            // Напр. "RTX 4090" от NVIDIA може да съществува само веднъж.
            bool exists = await _context.VideoCards.AnyAsync(v =>
                v.ModelName == model.VideoCard.ModelName &&
                v.ManufacturerId == model.VideoCard.ManufacturerId);

            if (exists)
            {
                // Добавяме грешка директно в ModelState — ще се покаже в изгледа
                ModelState.AddModelError("VideoCard.ModelName", "Тази видеокарта вече съществува за избрания производител.");
            }

            if (ModelState.IsValid)
            {
                // СТЪПКА 2: Качване на снимка (ако е избрана)
                if (model.ImageFile != null)
                {
                    // WebRootPath = абсолютният path до wwwroot/ папката
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

                    // GUID.NewGuid() генерира уникален идентификатор (напр. "3f2a8b1c-...").
                    // Prefixваме с него оригиналното файлово наименование,
                    // за да предотвратим колизии при еднакви имена.
                    string uniqueFileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // FileMode.Create = ако файлът съществува, го презаписва.
                    // CopyToAsync копира upload stream-а директно в disk файла.
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    // Записваме само файловото наименование (не пълния path)
                    model.VideoCard.ImageUrl = uniqueFileName;
                }

                // СТЪПКА 3: Добавяме VideoCard в DbSet и запазваме.
                // EF Core автоматично генерира Id при SaveChangesAsync().
                _context.VideoCards.Add(model.VideoCard);
                await _context.SaveChangesAsync();

                // СТЪПКА 4: Записваме избраните технологии.
                // model.VideoCard.Id вече е попълнен от EF Core след SaveChangesAsync().
                if (model.AvailableTechnologies != null && model.AvailableTechnologies.Any(t => t.IsSelected))
                {
                    var selectedTechs = model.AvailableTechnologies
                        .Where(t => t.IsSelected)  // Само отметнатите чекбоксове
                        .Select(t => new CardTechnology
                        {
                            VideoCardId = model.VideoCard.Id,
                            TechnologyId = t.TechnologyId
                        });

                    _context.CardTechnologies.AddRange(selectedTechs);
                    await _context.SaveChangesAsync();
                }

                // Пренасочваме към Admin списъка с карти
                return RedirectToAction(nameof(Index));
            }

            // СТЪПКА 5: Ако ModelState е невалиден — презареждаме формата.
            // Трябва да попълним отново SelectList и AvailableTechnologies,
            // защото те НЕ се изпращат обратно с POST формата.
            model.Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name", model.VideoCard.ManufacturerId);
            // AvailableTechnologies вече съдържа избора на потребителя от POST данните

            return View(model);
        }

        /// <summary>
        /// GET: /VideoCards/Delete/5
        /// Показва страница за потвърждение преди изтриване.
        /// Зарежда картата с нейния производител за информационен изглед.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            // id? е nullable — ако URL е /VideoCards/Delete без id, id ще е null
            if (id == null) return NotFound();

            var videoCard = await _context.VideoCards
                .Include(v => v.Manufacturer)  // Зареждаме производителя за показване в изгледа
                .FirstOrDefaultAsync(m => m.Id == id);

            if (videoCard == null) return NotFound();

            return View(videoCard);
        }

        /// <summary>
        /// POST: /VideoCards/Delete/5
        /// Потвърждава и извършва изтриването.
        /// ВАЖНО: Първо изтрива CardTechnology записите поради FK ограничение!
        /// Ако изтрием VideoCard директно — SQL хвърля грешка за нарушен FK.
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")] // ActionName позволява GET и POST да имат един и същ route name
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var videoCard = await _context.VideoCards.FindAsync(id);
            if (videoCard != null)
            {
                // Изтриваме всички CardTechnology записи за тази карта ПРЕДИ самата карта.
                // Без това — SQL Server ще хвърли Foreign Key Constraint Violation.
                var techLinks = _context.CardTechnologies.Where(ct => ct.VideoCardId == id);
                _context.CardTechnologies.RemoveRange(techLinks);

                _context.VideoCards.Remove(videoCard);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET: /VideoCards/Edit/5
        /// Зарежда форма за редактиране, предзаредена с текущите стойности.
        /// Особеност: зарежда и КОИ технологии са избрани (IsSelected = true).
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Зареждаме картата заедно с нейните технологии
            var videoCard = await _context.VideoCards
                .Include(v => v.CardTechnologies)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (videoCard == null) return NotFound();

            // Материализираме (ToList) Id-тата в локален списък в паметта.
            // ВАЖНО: Ако използваме директно videoCard.CardTechnologies в LINQ
            // заявката долу — EF Core може да се обърка и да направи неправилна SQL.
            var selectedTechIds = videoCard.CardTechnologies.Select(ct => ct.TechnologyId).ToList();

            var viewModel = new VideoCardCreateViewModel
            {
                VideoCard = videoCard,
                Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name", videoCard.ManufacturerId),
                AvailableTechnologies = _context.Technologies.Select(t => new TechnologySelection
                {
                    TechnologyId = t.Id,
                    Name = t.Name,
                    // Contains() проверява в локалния List<int> — не в базата
                    IsSelected = selectedTechIds.Contains(t.Id)
                }).ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// POST: /VideoCards/Edit/5
        /// Запазва промените по видеокартата.
        /// Включва:
        ///   - Опционална смяна на снимката
        ///   - Пълна РЕСИНХРОНИЗАЦИЯ на технологиите (изтрива старите, добавя нови)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VideoCardCreateViewModel model)
        {
            // Проверка дали ID от URL съвпада с ID в модела
            if (id != model.VideoCard.Id) return NotFound();

            // Махаме полетата, които не идват от формата
            ModelState.Remove("VideoCard.Manufacturer");
            ModelState.Remove("VideoCard.CardTechnologies");
            ModelState.Remove("VideoCard.AddedById");
            ModelState.Remove("ImageFile"); // При Edit снимката не е задължителна

            // Проверка за дубликат, ИЗКЛЮЧВАЙКИ текущата карта (не трябва да сравняваме сама с себе си)
            bool exists = await _context.VideoCards.AnyAsync(v =>
                v.ModelName == model.VideoCard.ModelName &&
                v.ManufacturerId == model.VideoCard.ManufacturerId &&
                v.Id != id);  // <— ключово: изключваме текущата карта

            if (exists)
            {
                ModelState.AddModelError("VideoCard.ModelName", "Вече съществува друга видеокарта с това име.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // AsNoTracking() — четем старите данни без да ги "проследяваме".
                    // Нужно е за да вземем ImageUrl и AddedById без конфликт с update-а.
                    var existingCard = await _context.VideoCards
                        .AsNoTracking()
                        .FirstOrDefaultAsync(v => v.Id == id);

                    if (model.ImageFile != null)
                    {
                        // Ако е качена нова снимка — същата логика като Create
                        // (GUID prefix + запис в wwwroot/images/)
                        // Тук логиката е опусната в кода, но трябва да е същата като Create
                    }
                    else
                    {
                        // Ако няма нова снимка — запазваме старото ImageUrl
                        model.VideoCard.ImageUrl = existingCard.ImageUrl;
                    }

                    // Запазваме и AddedById от оригинала (не е в формата)
                    model.VideoCard.AddedById = existingCard.AddedById;

                    // _context.Update() маркира ВСИЧКИ полета за update (не само промените)
                    _context.Update(model.VideoCard);

                    // РЕСИНХРОНИЗАЦИЯ НА ТЕХНОЛОГИИТЕ:
                    // Стратегия: изтриваме ВСИЧКИ стари и добавяме ВСИЧКИ нови.
                    // По-просто от намирането на разликите.
                    var oldLinks = _context.CardTechnologies.Where(ct => ct.VideoCardId == id);
                    _context.CardTechnologies.RemoveRange(oldLinks);

                    if (model.AvailableTechnologies != null)
                    {
                        var newLinks = model.AvailableTechnologies
                            .Where(t => t.IsSelected)
                            .Select(t => new CardTechnology { VideoCardId = id, TechnologyId = t.TechnologyId });
                        _context.CardTechnologies.AddRange(newLinks);
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Възникна грешка при записа: " + ex.Message);
                }
            }

            // При грешка — презареждаме SelectList (не се пази в POST данните)
            model.Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name", model.VideoCard.ManufacturerId);
            return View(model);
        }

        // ═══════════════════════════════════════════════════════
        // КЛИЕНТСКИ ФУНКЦИИ
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// GET: /VideoCards/UserIndex?searchTerm=RTX&manufacturerId=1
        /// Клиентски каталог с динамично филтриране.
        /// Параметрите идват от GET формата (query string).
        /// </summary>
        public async Task<IActionResult> UserIndex(string searchTerm, int? manufacturerId)
        {
            // AsQueryable() — не изпълнява SQL веднага, а изгражда заявката стъпка по стъпка.
            // Позволява условно добавяне на WHERE клаузи.
            var query = _context.VideoCards
                .Include(v => v.Manufacturer)
                .AsQueryable();

            // Добавяме WHERE за търсене само ако е въведен текст
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Contains() → SQL LIKE '%searchTerm%' — регистро-независимо в SQL Server
                query = query.Where(v => v.ModelName.Contains(searchTerm));
            }

            // Добавяме WHERE за производител само ако е избран
            if (manufacturerId.HasValue)
            {
                query = query.Where(v => v.ManufacturerId == manufacturerId.Value);
            }

            // ViewBag предава данни към изгледа без строга типизация.
            // Manufacturers — за падащото меню с всички марки
            // CurrentSearch и CurrentManufacturer — за запазване на текущите филтри
            ViewBag.Manufacturers = await _context.Manufacturers.ToListAsync();
            ViewBag.CurrentSearch = searchTerm;
            ViewBag.CurrentManufacturer = manufacturerId;

            // ToListAsync() ТОГАВА изпълнява SQL с всички натрупани WHERE клаузи
            var results = await query.ToListAsync();
            return View(results);
        }

        /// <summary>
        /// GET: /VideoCards/UserDetails/5
        /// Клиентска детайлна страница за конкретна видеокарта.
        /// Зарежда технологиите И коментарите с техните автори.
        /// </summary>
        public async Task<IActionResult> UserDetails(int? id)
        {
            if (id == null) return NotFound();

            var videoCard = await _context.VideoCards
                .Include(v => v.Manufacturer)
                .Include(v => v.CardTechnologies)
                    .ThenInclude(ct => ct.Technology) // Зарежда Technology от CardTechnology
                .FirstOrDefaultAsync(m => m.Id == id);

            if (videoCard == null) return NotFound();

            // Зареждаме коментарите заедно с потребителите, написали ги.
            // OrderByDescending — най-новите първо (обратен хронологичен ред).
            var comments = await _context.Comments
                .Include(c => c.User)  // JOIN към AspNetUsers за Email на автора
                .Where(c => c.VideoCardId == id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Използваме VideoCardDetailsViewModel вместо директен VideoCard модел,
            // защото трябва да предадем и двата обекта (карта + коментари).
            var viewModel = new VideoCardDetailsViewModel
            {
                VideoCard = videoCard,
                Comments = comments
            };

            return View(viewModel);
        }

        /// <summary>
        /// GET: /VideoCards/AddComment?cardId=5&content=...
        /// Добавя коментар към видеокарта. Само за клиенти!
        /// Admin получава 403 Forbid — не може да коментира.
        /// </summary>
        public async Task<IActionResult> AddComment(int cardId, string content)
        {
            // Бизнес правило: Администраторите не коментират продукти.
            // Forbid() → HTTP 403 Forbidden
            if (User.IsInRole("Admin")) return Forbid();

            // Ако съдържанието е празно — просто пренасочваме без добавяне
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("UserDetails", new { id = cardId });

            // ClaimTypes.NameIdentifier = уникалния Id на потребителя в Identity системата
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var comment = new Comment
            {
                Content = content,
                VideoCardId = cardId,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Пренасочваме обратно към детайлната страница след добавяне
            return RedirectToAction("UserDetails", new { id = cardId });
        }
    }
}
