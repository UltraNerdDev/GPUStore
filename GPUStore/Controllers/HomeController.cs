// ============================================================
// Controllers/HomeController.cs — Начална страница и системни операции
// ============================================================
// HomeController управлява:
//   1. Началната страница (Index) — адаптивна спрямо ролята
//   2. Зареждане на демо данни (SeedDatabase) — само Admin
//   3. Изчистване на базата (ClearDatabase) — само Admin
//   4. Privacy страница и Error страница
// ============================================================

using System.Diagnostics;
using GPUStore.Data;
using GPUStore.Models;
using GPUStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Controllers
{
    public class HomeController : Controller
    {
        // _context дава достъп до базата данни чрез EF Core
        private readonly ApplicationDbContext _context;

        // _logger се използва за логване на грешки и debug информация
        private readonly ILogger<HomeController> _logger;

        /// Конструкторът приема зависимостите чрез Dependency Injection.
        /// ASP.NET Core автоматично ги инжектира при създаване на контролера.
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        /// GET: /  или  GET: /Home/Index
        /// Начална страница — показва различен изглед спрямо ролята:
        ///   - Admin: AdminIndex.cshtml с Dashboard статистика
        ///   - Клиент/Гост: Index.cshtml с маркетинг съдържание
        public async Task<IActionResult> Index()
        {
            // Проверяваме дали потребителят е автентикиран И е Admin.
            // User.Identity.IsAuthenticated — дали е влязъл изобщо
            // User.IsInRole("Admin") — дали е в Admin роля
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                // Изграждаме Dashboard статистика чрез 4 паралелни async заявки към базата.
                // SumAsync(o => o.TotalPrice) — сумира TotalPrice на всички поръчки (общ оборот).
                // CountAsync() — брои записи в съответните таблици.
                var stats = new AdminDashboardViewModel
                {
                    TotalVideoCards = await _context.VideoCards.CountAsync(),
                    TotalOrders = await _context.Orders.CountAsync(),
                    TotalManufacturers = await _context.Manufacturers.CountAsync(),
                    TotalRevenue = await _context.Orders.SumAsync(o => o.TotalPrice)
                };

                // Подаваме AdminIndex.cshtml изрично по наименование + данните
                return View("AdminIndex", stats);
            }

            // За клиенти и гости — стандартния Index.cshtml (маркетинг страница)
            return View();
        }

        /// GET: /Home/SeedDatabase
        /// Зарежда демо данни (производители, технологии, видеокарти, релации).
        /// САМО ЗА ADMIN. Идемпотентен — безопасен за многократно извикване.
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SeedDatabase()
        {
            // HttpContext.RequestServices е DI container-ът на текущата заявка.
            // Подаваме го на SeederClass, за да може да вземе DbContext от него.
            await SeederClass.Initialize(HttpContext.RequestServices);

            // След успешния seed пренасочваме към каталога с видеокарти
            return RedirectToAction("Index", "Home", new { message = "Данните са заредени успешно!" });
        }

        /// POST: /Home/ClearDatabase
        /// Изтрива ВСИЧКИ продукти, поръчки, производители и технологии от базата.
        /// САМО ЗА ADMIN. НЕОБРАТИМА ОПЕРАЦИЯ!
        ///
        /// Важно: Използва POST (не GET) за защита от случайно изтриване
        /// чрез директен URL достъп. Допълнително защитен с [ValidateAntiForgeryToken].
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken] // Защита срещу CSRF атаки
        public async Task<IActionResult> ClearDatabase()
        {
            try
            {
                // ВАЖЕН РЕД НА ИЗТРИВАНЕ: заради Foreign Key ограниченията
                // трябва първо да изтрием "дъщерните" таблици, след това "родителските".
                // Ако опитаме да изтрием Manufacturer преди VideoCards — SQL хвърля FK error.

                // 1. Първо: свързващите таблици (без FK проблеми при изтриване на родителите)
                _context.CardTechnologies.RemoveRange(_context.CardTechnologies);
                _context.OrderItems.RemoveRange(_context.OrderItems);

                // 2. После: основните таблици
                _context.VideoCards.RemoveRange(_context.VideoCards);
                _context.Manufacturers.RemoveRange(_context.Manufacturers);
                _context.Technologies.RemoveRange(_context.Technologies);
                _context.Orders.RemoveRange(_context.Orders);

                // Запазваме всички изтривания в ЕДНА SQL транзакция
                await _context.SaveChangesAsync();

                // TempData се пази в cookie за следващата заявка (след redirect)
                TempData["SuccessMessage"] = "Базата беше изцяло изчистена успешно.";
            }
            catch (Exception ex)
            {
                // При грешка (напр. FK constraint нарушение) показваме съобщение
                TempData["ErrorMessage"] = "Грешка при изчистването: " + ex.Message;
            }

            // Пренасочваме към Admin видеокарти страницата
            return RedirectToAction("Index", "Home");
        }

        /// GET: /Home/Privacy
        /// Статична страница с политика за поверителност.
        public IActionResult Privacy()
        {
            return View();
        }

        /// GET: /Home/Error
        /// Показва страница за грешки с уникален RequestId за debugging.
        /// [ResponseCache] указва на браузъра да не кешира тази страница.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Activity.Current?.Id — уникален Id от .NET Activity трacing система
            // ?? HttpContext.TraceIdentifier — fallback: ASP.NET Core trace id
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
