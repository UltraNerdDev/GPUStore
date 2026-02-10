using GPUStore.Data;
using GPUStore.Models;
using GPUStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Controllers
{
    [Authorize(Roles = "Admin")]
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

        public async Task<IActionResult> Index()
        {
            var cards = await _context.VideoCards
                .Include(v => v.Manufacturer)
                .Include(v => v.CardTechnologies)
                    .ThenInclude(ct => ct.Technology)
                .ToListAsync();
            return View(cards);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VideoCardCreateViewModel model)
        {
            // Премахваме навигационните свойства от валидацията
            ModelState.Remove("VideoCard.Manufacturer");
            ModelState.Remove("VideoCard.CardTechnologies");

            if (ModelState.IsValid)
            {
                // 1. ЛОГИКА ЗА КАЧВАНЕ НА СНИМКА
                if (model.ImageFile != null)
                {
                    // Намираме пътя до wwwroot/images
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

                    // Създаваме уникално име: GUID + името на файла
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;

                    // Пълният път до мястото, където ще се запише файла
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Записваме файла на диска
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    // Записваме САМО името на файла в базата данни
                    model.VideoCard.ImageUrl = uniqueFileName;
                }

                // 2. ЗАПИС НА ВИДЕОКАРТАТА
                _context.VideoCards.Add(model.VideoCard);
                await _context.SaveChangesAsync();

                // 3. ЗАПИС НА ТЕХНОЛОГИИТЕ (Много-към-Много)
                if (model.AvailableTechnologies != null)
                {
                    foreach (var tech in model.AvailableTechnologies.Where(t => t.IsSelected))
                    {
                        var cardTech = new CardTechnology
                        {
                            VideoCardId = model.VideoCard.Id,
                            TechnologyId = tech.TechnologyId
                        };
                        _context.CardTechnologies.Add(cardTech);
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            // Ако моделът не е валиден, презареждаме списъците
            model.Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name", model.VideoCard.ManufacturerId);
            model.AvailableTechnologies = _context.Technologies.Select(t => new TechnologySelection
            {
                TechnologyId = t.Id,
                Name = t.Name,
                IsSelected = false
            }).ToList();

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

            ModelState.Remove("VideoCard.Manufacturer");
            ModelState.Remove("VideoCard.CardTechnologies");

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. ОБРАБОТКА НА СНИМКАТА
                    if (model.ImageFile != null)
                    {
                        // Път до папката
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");

                        // ИЗТРИВАНЕ НА СТАРАТА СНИМКА (ако съществува)
                        // Първо трябва да вземем името на старата снимка без проследяване (AsNoTracking)
                        var oldCard = await _context.VideoCards.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
                        if (oldCard != null && !string.IsNullOrEmpty(oldCard.ImageUrl))
                        {
                            string oldFilePath = Path.Combine(uploadsFolder, oldCard.ImageUrl);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        // ЗАПИС НА НОВАТА СНИМКА
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                        string newFilePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(newFilePath, FileMode.Create))
                        {
                            await model.ImageFile.CopyToAsync(fileStream);
                        }
                        model.VideoCard.ImageUrl = uniqueFileName;
                    }
                    else
                    {
                        // Ако не е качен нов файл, запазваме стария ImageUrl
                        // Трябва да вземем стойността от базата, защото формата не я изпраща автоматично
                        var currentCard = await _context.VideoCards.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
                        model.VideoCard.ImageUrl = currentCard?.ImageUrl;
                    }

                    // 2. ОБНОВЯВАНЕ НА КАРТАТА
                    _context.Update(model.VideoCard);

                    // 3. ТЕХНОЛОГИИ (Изтриваме и добавяме наново)
                    var oldLinks = _context.CardTechnologies.Where(ct => ct.VideoCardId == id);
                    _context.CardTechnologies.RemoveRange(oldLinks);
                    await _context.SaveChangesAsync();

                    if (model.AvailableTechnologies != null)
                    {
                        foreach (var tech in model.AvailableTechnologies.Where(t => t.IsSelected))
                        {
                            _context.CardTechnologies.Add(new CardTechnology { VideoCardId = id, TechnologyId = tech.TechnologyId });
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

            model.Manufacturers = new SelectList(_context.Manufacturers, "Id", "Name", model.VideoCard.ManufacturerId);
            return View(model);
        }
    }
}
