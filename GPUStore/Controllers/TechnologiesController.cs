using GPUStore.Data;
using GPUStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TechnologiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TechnologiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>GET: /Technologies — Списък с всички технологии</summary>
        public IActionResult Index()
        {
            var techs = _context.Technologies.ToList();
            return View(techs);
        }

        /// <summary>GET: /Technologies/Create — Форма за нова технология</summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// POST: /Technologies/Create
        /// Проверява за дублиращо наименование.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Technology technology)
        {
            bool exists = await _context.Technologies.AnyAsync(t => t.Name == technology.Name);

            if (exists)
            {
                ModelState.AddModelError("Name", "Тази технология вече съществува в базата.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(technology);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(technology);
        }

        /// <summary>GET: /Technologies/Edit/5</summary>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var technology = await _context.Technologies.FindAsync(id);
            if (technology == null) return NotFound();
            return View(technology);
        }

        /// <summary>
        /// POST: /Technologies/Edit/5
        /// Проверка за дубликат с изключение на текущата технология.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Technology technology)
        {
            if (id != technology.Id) return NotFound();

            // Проверяваме дали съществува ДРУГА технология с СЪЩОТО Name
            bool exists = await _context.Technologies
                .AnyAsync(m => m.Name == technology.Name && m.Id != id);

            if (exists)
            {
                ModelState.AddModelError("Name", "Тази технология вече съществува в базата.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(technology);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(technology);
        }

        /// <summary>GET: /Technologies/Delete/5</summary>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var technology = await _context.Technologies.FirstOrDefaultAsync(m => m.Id == id);
            if (technology == null) return NotFound();
            return View(technology);
        }

        /// <summary>
        /// POST: /Technologies/Delete/5
        /// Изтрива технологията.
        /// ВНИМАНИЕ: Ако технологията е свързана с карти в CardTechnologies — FK error!
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var technology = await _context.Technologies.FindAsync(id);
            if (technology != null)
            {
                _context.Technologies.Remove(technology);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

