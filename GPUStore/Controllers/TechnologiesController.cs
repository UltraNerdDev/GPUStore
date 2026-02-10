using GPUStore.Data;
using GPUStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    }
}
