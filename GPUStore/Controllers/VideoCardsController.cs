using GPUStore.Data;
using GPUStore.Models;
using GPUStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VideoCardsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VideoCardsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Списък с всички видеокарти (Index)
        public async Task<IActionResult> Index()
        {
            // Използваме .Include(), за да заредим и името на производителя
            var cards = await _context.VideoCards
                .Include(v => v.Manufacturer)
                .Include(v => v.CardTechnologies) // Зареждаме междинната таблица
                    .ThenInclude(ct => ct.Technology) // Зареждаме самото име на технологията
                .ToListAsync();
            return View(cards);
        }

        // 2. Форма за създаване (GET)
        public IActionResult Create()
        {
            var viewModel = new VideoCardCreateViewModel
            {
                // Пълним падащото меню с производители
                Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name"),

                // Превръщаме всички технологии от базата в списък с чекбоксове
                AvailableTechnologies = _context.Technologies.Select(t => new TechnologySelection
                {
                    TechnologyId = t.Id,
                    Name = t.Name,
                    IsSelected = false
                }).ToList()
            };

            return View(viewModel);
        }

        // 3. Записване на видеокартата (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VideoCardCreateViewModel model)
        {
            // Махаме валидацията за навигационните свойства, които не идват от формата
            ModelState.Remove("VideoCard.Manufacturer");
            ModelState.Remove("VideoCard.CardTechnologies");

            if (ModelState.IsValid)
            {
                // Стъпка А: Записваме основната карта
                _context.VideoCards.Add(model.VideoCard);
                await _context.SaveChangesAsync();

                // Стъпка Б: Записваме избраните технологии в свързващата таблица
                if (model.AvailableTechnologies != null)
                {
                    foreach (var tech in model.AvailableTechnologies.Where(t => t.IsSelected))
                    {
                        var cardTech = new CardTechnology
                        {
                            VideoCardId = model.VideoCard.Id, // Вече има Id след SaveChanges
                            TechnologyId = tech.TechnologyId
                        };
                        _context.CardTechnologies.Add(cardTech);
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            // Ако моделът не е валиден, презареждаме данните за формата
            model.Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name", model.VideoCard.ManufacturerId);
            return View(model);
        }

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

        // POST: VideoCards/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, VideoCardCreateViewModel model)
        {
            if (id != model.VideoCard.Id) return NotFound();

            // Махаме валидацията за обекти, които не идват от формата
            ModelState.Remove("VideoCard.Manufacturer");
            ModelState.Remove("VideoCard.CardTechnologies");

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Обновяваме основната информация
                    _context.Update(model.VideoCard);

                    // 2. Изтриваме старите връзки с технологии за тази карта
                    var oldLinks = _context.CardTechnologies.Where(ct => ct.VideoCardId == id);
                    _context.CardTechnologies.RemoveRange(oldLinks);
                    await _context.SaveChangesAsync();

                    // 3. Добавяме новите избрани технологии
                    if (model.AvailableTechnologies != null)
                    {
                        foreach (var tech in model.AvailableTechnologies.Where(t => t.IsSelected))
                        {
                            _context.CardTechnologies.Add(new CardTechnology
                            {
                                VideoCardId = id,
                                TechnologyId = tech.TechnologyId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.VideoCards.Any(e => e.Id == model.VideoCard.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // Ако има грешка, презареждаме падащото меню
            model.Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name", model.VideoCard.ManufacturerId);
            return View(model);
        }
    }
}
