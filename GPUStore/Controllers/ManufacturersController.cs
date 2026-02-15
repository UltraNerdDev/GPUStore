// ============================================================
// Controllers/ManufacturersController.cs — CRUD за производители
// ============================================================
// Пълен CRUD само за Admin. Всеки метод проверява за дубликати.
// [Authorize(Roles = "Admin")] на ниво КЛАС = всички методи са Admin-only.
// ============================================================

using GPUStore.Data;
using GPUStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Controllers
{
    // Целият контролер е достъпен САМО за Admin
    [Authorize(Roles = "Admin")]
    public class ManufacturersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManufacturersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// GET: /Manufacturers — Списък с всички производители
        public IActionResult Index()
        {
            // ToList() (не async) — простата синхронна версия е достатъчна тук
            var manufacturers = _context.Manufacturers.ToList();
            return View(manufacturers);
        }

        /// GET: /Manufacturers/Create — Форма за нов производител
        public IActionResult Create()
        {
            return View();
        }

        /// POST: /Manufacturers/Create
        /// Проверява за дублиращо наименование ПРЕДИ запис.
        /// [Bind("Id,Name")] — приема само тези 2 полета от POST (security best practice).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] Manufacturer manufacturer)
        {
            // Проверка за дубликат по Name (независимо от главни/малки букви в SQL Server)
            bool exists = await _context.Manufacturers.AnyAsync(t => t.Name == manufacturer.Name);

            if (exists)
            {
                // Добавяме грешка директно към ModelState.
                // Ключът "Name" съответства на полето в изгледа — грешката ще се покаже под него.
                ModelState.AddModelError("Name", "Този производител вече съществува в базата.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(manufacturer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(manufacturer);
        }

        /// GET: /Manufacturers/Edit/5 — Форма за редактиране
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var manufacturer = await _context.Manufacturers.FindAsync(id);
            if (manufacturer == null) return NotFound();
            return View(manufacturer);
        }

        /// POST: /Manufacturers/Edit/5
        /// Проверява за дубликат ИЗКЛЮЧВАЙКИ текущия запис.
        /// Без "v.Id != id" проверката щеше да гърми при запис без промяна на Name.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Manufacturer manufacturer)
        {
            if (id != manufacturer.Id) return NotFound();

            // Проверка за дубликат, ИЗКЛЮЧВАЙКИ текущия производител (m.Id != id)
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

        /// GET: /Manufacturers/Delete/5 — Потвърждение за изтриване
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var manufacturer = await _context.Manufacturers.FirstOrDefaultAsync(m => m.Id == id);
            if (manufacturer == null) return NotFound();
            return View(manufacturer);
        }

        /// POST: /Manufacturers/Delete/5 — Извършва изтриването.
        /// ВНИМАНИЕ: Ако производителят има свързани VideoCards — SQL ще хвърли FK error!
        /// Трябва първо да изтриете или преместите картите.
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