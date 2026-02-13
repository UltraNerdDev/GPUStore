using GPUStore.Data;
using GPUStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ManufacturersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManufacturersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Списък с всички производители
        public IActionResult Index()
        {
            var manufacturers = _context.Manufacturers.ToList();
            return View(manufacturers);
        }

        // Страница за добавяне (GET)
        public IActionResult Create()
        {
            return View();
        }

        // Записване в базата (POST)
        //[HttpPost]
        //public IActionResult Create(Manufacturer manufacturer)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _context.Manufacturers.Add(manufacturer);
        //        _context.SaveChanges();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(manufacturer);
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Manufacturer manufacturer)
        {
            // 1. Проверка за дубликат
            bool exists = await _context.Manufacturers.AnyAsync(t => t.Name == manufacturer.Name);

            if (exists)
            {
                // Ръчно добавяме грешка към ModelState, която ще се появи под полето Name
                ModelState.AddModelError("Name", "Този производител вече съществува в базата.");
            }

            // 2. Стандартната проверка
            if (ModelState.IsValid)
            {
                _context.Add(manufacturer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(manufacturer);
        }

        // GET: Manufacturers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var manufacturer = await _context.Manufacturers.FindAsync(id);
            if (manufacturer == null) return NotFound();
            return View(manufacturer);
        }

        // POST: Manufacturers/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, Manufacturer manufacturer)
        //{
        //    if (id != manufacturer.Id) return NotFound();

        //    if (ModelState.IsValid)
        //    {
        //        _context.Update(manufacturer);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(manufacturer);
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Manufacturer manufacturer)
        {
            if (id != manufacturer.Id) return NotFound();

            // Проверка за дублиране на име
            bool exists = await _context.Manufacturers
                .AnyAsync(m => m.Name == manufacturer.Name && m.Id != id);

            if (exists)
            {
                ModelState.AddModelError("Name", "Вече съществува производител с това име.");
            }

            if (ModelState.IsValid)
            {
                _context.Update(manufacturer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(manufacturer);
        }

        // GET: Manufacturers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var manufacturer = await _context.Manufacturers.FirstOrDefaultAsync(m => m.Id == id);
            if (manufacturer == null) return NotFound();
            return View(manufacturer);
        }

        // POST: Manufacturers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var manufacturer = await _context.Manufacturers.FindAsync(id);
            if (manufacturer != null)
            {
                _context.Manufacturers.Remove(manufacturer);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
