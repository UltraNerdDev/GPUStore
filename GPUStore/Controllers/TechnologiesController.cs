using GPUStore.Data;
using GPUStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Controllers
{
    [Authorize(Roles = "Admin")] // Само админ може да пипа тук
    public class TechnologiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TechnologiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Списък с всички технологии
        public IActionResult Index()
        {
            var techs = _context.Technologies.ToList();
            return View(techs);
        }

        // 2. Форма за добавяне (GET)
        public IActionResult Create()
        {
            return View();
        }

        // 3. Записване в базата (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Technology technology)
        {
            if (ModelState.IsValid)
            {
                _context.Technologies.Add(technology);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(technology);
        }

        // GET: Technologies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var technology = await _context.Technologies.FindAsync(id);
            if (technology == null) return NotFound();

            return View(technology);
        }

        // POST: Technologies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Technology technology)
        {
            if (id != technology.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(technology);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(technology);
        }

        // GET: Technologies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var technology = await _context.Technologies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (technology == null) return NotFound();

            return View(technology);
        }

        // POST: Technologies/Delete/5
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
