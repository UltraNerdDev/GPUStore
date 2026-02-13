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
    [Authorize]
    public class VideoCardsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Конструкторът вече приема и средата, за да достъпваме wwwroot
        public VideoCardsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var cards = await _context.VideoCards
                .Include(v => v.Manufacturer)
                .Include(v => v.CardTechnologies)
                    .ThenInclude(ct => ct.Technology)
                .ToListAsync();
            return View(cards);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            var viewModel = new VideoCardCreateViewModel
            {
                Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name"),
                AvailableTechnologies = _context.Technologies.Select(t => new TechnologySelection
                {
                    TechnologyId = t.Id,
                    Name = t.Name,
                    IsSelected = false
                }).ToList()
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VideoCardCreateViewModel model)
        {
            // Премахваме навигационните свойства, за да не гърми валидацията
            ModelState.Remove("VideoCard.Manufacturer");
            ModelState.Remove("VideoCard.CardTechnologies");

            // 1. ПРОВЕРКА ЗА ДУБЛИКАТ (Преди ModelState.IsValid)
            bool exists = await _context.VideoCards.AnyAsync(v =>
                v.ModelName == model.VideoCard.ModelName &&
                v.ManufacturerId == model.VideoCard.ManufacturerId);

            if (exists)
            {
                ModelState.AddModelError("VideoCard.ModelName", "Тази видеокарта вече съществува за избрания производител.");
            }

            if (ModelState.IsValid)
            {
                // 2. ЛОГИКА ЗА КАЧВАНЕ НА СНИМКА
                if (model.ImageFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

                    // Генерираме уникално име, за да не се застъпват файловете
                    string uniqueFileName = $"{Guid.NewGuid()}_{model.ImageFile.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    model.VideoCard.ImageUrl = uniqueFileName;
                }

                // Взимаме ID-то на админа (ако моделът ти поддържа AddedById)
                // model.VideoCard.AddedById = _userManager.GetUserId(User);

                // 3. ЗАПИС НА ВИДЕОКАРТАТА
                _context.VideoCards.Add(model.VideoCard);
                await _context.SaveChangesAsync();

                // 4. ЗАПИС НА ТЕХНОЛОГИИТЕ (Много-към-Много)
                if (model.AvailableTechnologies != null && model.AvailableTechnologies.Any(t => t.IsSelected))
                {
                    var selectedTechs = model.AvailableTechnologies
                        .Where(t => t.IsSelected)
                        .Select(t => new CardTechnology
                        {
                            VideoCardId = model.VideoCard.Id,
                            TechnologyId = t.TechnologyId
                        });

                    _context.CardTechnologies.AddRange(selectedTechs);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            // 5. ПРЕЗАРЕЖДАНЕ ПРИ ГРЕШКА
            // Ако сме тук, значи нещо се е объркало. Пълним списъците отново.
            model.Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name", model.VideoCard.ManufacturerId);

            // Важно: Запазваме избора на админа за технологиите, за да не ги цъка наново!
            // Вече имаме избраните в модела, просто ги показваме пак.
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        // GET: VideoCards/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var videoCard = await _context.VideoCards
                .Include(v => v.Manufacturer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (videoCard == null) return NotFound();

            return View(videoCard);
        }

        [Authorize(Roles = "Admin")]
        // POST: VideoCards/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var videoCard = await _context.VideoCards.FindAsync(id);
            if (videoCard != null)
            {
                // Първо изтриваме връзките с технологиите, за да не гърми базата (Foreign Key constraint)
                var techLinks = _context.CardTechnologies.Where(ct => ct.VideoCardId == id);
                _context.CardTechnologies.RemoveRange(techLinks);

                _context.VideoCards.Remove(videoCard);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        // Подобреният GET: VideoCards/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var videoCard = await _context.VideoCards
                .Include(v => v.CardTechnologies)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (videoCard == null) return NotFound();

            // Важно: Вземаме ID-тата в паметта, за да не се бърка LINQ
            var selectedTechIds = videoCard.CardTechnologies.Select(ct => ct.TechnologyId).ToList();

            var viewModel = new VideoCardCreateViewModel
            {
                VideoCard = videoCard,
                Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name", videoCard.ManufacturerId),
                AvailableTechnologies = _context.Technologies.Select(t => new TechnologySelection
                {
                    TechnologyId = t.Id,
                    Name = t.Name,
                    IsSelected = selectedTechIds.Contains(t.Id) // Сравняваме с локалния списък
                }).ToList()
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VideoCardCreateViewModel model)
        {
            // 1. Важно: Синхронизиране на ID-тата
            if (id != model.VideoCard.Id) return NotFound();

            // 2. Махаме всичко, което не е част от формата
            ModelState.Remove("VideoCard.Manufacturer");
            ModelState.Remove("VideoCard.CardTechnologies");
            ModelState.Remove("VideoCard.AddedById");
            ModelState.Remove("ImageFile"); // При Едит не е задължително да качваш нова снимка

            // 3. Проверка за дубликат (игнорирайки текущата карта)
            bool exists = await _context.VideoCards.AnyAsync(v =>
                v.ModelName == model.VideoCard.ModelName &&
                v.ManufacturerId == model.VideoCard.ManufacturerId &&
                v.Id != id);

            if (exists)
            {
                ModelState.AddModelError("VideoCard.ModelName", "Вече съществува друга видеокарта с това име.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Вземаме инстанцията от базата за снимката
                    var existingCard = await _context.VideoCards.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);

                    if (model.ImageFile != null)
                    {
                        // ... (Логиката за качване на нова снимка, която написахме по-рано) ...
                    }
                    else
                    {
                        model.VideoCard.ImageUrl = existingCard.ImageUrl;
                    }

                    // Ръчно прехвърляме AddedById, ако не е в скрито поле
                    model.VideoCard.AddedById = existingCard.AddedById;

                    _context.Update(model.VideoCard);

                    // Технологиите...
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

            // Ако сме стигнали до тук, значи ModelState не е валиден!
            model.Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name", model.VideoCard.ManufacturerId);
            return View(model);
        }

        // Визуализира каталога за клиенти
        //public async Task<IActionResult> UserIndex()
        //{
        //    var cards = await _context.VideoCards.Include(v => v.Manufacturer).ToListAsync();
        //    return View("UserIndex", cards);
        //}
        public async Task<IActionResult> UserIndex(string searchTerm, int? manufacturerId)
        {
            // Започваме с базовата заявка
            var query = _context.VideoCards
                .Include(v => v.Manufacturer)
                .AsQueryable();

            // Филтър по име (търсачка)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(v => v.ModelName.Contains(searchTerm));
            }

            // Филтър по производител
            if (manufacturerId.HasValue)
            {
                query = query.Where(v => v.ManufacturerId == manufacturerId.Value);
            }

            // Вземаме списъка с производители за падащото меню
            ViewBag.Manufacturers = await _context.Manufacturers.ToListAsync();
            ViewBag.CurrentSearch = searchTerm;
            ViewBag.CurrentManufacturer = manufacturerId;

            var results = await query.ToListAsync();
            return View(results);
        }

        // ДОБАВИ ТОВА: Детайли за клиенти
        public async Task<IActionResult> UserDetails(int? id)
        {
            if (id == null) return NotFound();

            var videoCard = await _context.VideoCards
                .Include(v => v.Manufacturer)
                .Include(v => v.CardTechnologies).ThenInclude(ct => ct.Technology)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (videoCard == null) return NotFound();

            // Вземаме коментарите за тази карта
            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.VideoCardId == id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var viewModel = new VideoCardDetailsViewModel
            {
                VideoCard = videoCard,
                Comments = comments
            };

            return View(viewModel); // Увери се, че имаш Views/VideoCards/UserDetails.cshtml
        }

        public async Task<IActionResult> AddComment(int cardId, string content)
        {
            // Проверка: Админите не могат да пишат коментари
            if (User.IsInRole("Admin")) return Forbid();

            if (string.IsNullOrWhiteSpace(content)) return RedirectToAction("UserDetails", new { id = cardId });

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

            return RedirectToAction("UserDetails", new { id = cardId });
        }
    }
}
