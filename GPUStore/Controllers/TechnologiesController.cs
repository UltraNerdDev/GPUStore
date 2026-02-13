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
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Create(Technology technology)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _context.Technologies.Add(technology);
        //        _context.SaveChanges();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(technology);
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Technology technology)
        {
            // 1. Проверка за дубликат
            bool exists = await _context.Technologies.AnyAsync(t => t.Name == technology.Name);

            if (exists)
            {
                // Ръчно добавяме грешка към ModelState, която ще се появи под полето Name
                ModelState.AddModelError("Name", "Тази технология вече съществува в базата.");
            }

            // 2. Стандартната проверка
            if (ModelState.IsValid)
            {
                _context.Add(technology);
                await _context.SaveChangesAsync();
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
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, Technology technology)
        //{
        //    if (id != technology.Id) return NotFound();

        //    if (ModelState.IsValid)
        //    {
        //        _context.Update(technology);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(technology);
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Technology technology)
        {
            if (id != technology.Id) return NotFound();

            // Проверка за дублиране на име
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
