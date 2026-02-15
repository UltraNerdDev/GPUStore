// ============================================================
// Controllers/OrdersController.cs — Управление на поръчки
// ============================================================
// Разделен на два слоя:
//   ADMIN: Index (всички), Details (конкретна), UpdateStatus (статус)
//   КЛИЕНТ: Checkout (преглед), ConfirmOrder (финализиране), MyOrders (история)
// ============================================================

using GPUStore.Data;
using GPUStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GPUStore.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /Orders  (ADMIN)
        /// Списък с всички поръчки, сортирани от най-новите.
        /// Include(o => o.User) — зарежда Email на клиента за показване.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User)        // JOIN към AspNetUsers за Email
                .Include(o => o.OrderItems)  // Зарежда елементите (за изчисляване на суми в изгледа)
                .OrderByDescending(o => o.OrderDate)  // Последните поръчки първо
                .ToListAsync();
            return View(orders);
        }

        /// <summary>
        /// GET: /Orders/Details/5  (ADMIN)
        /// Детайлна страница за конкретна поръчка с всички продукти.
        /// ThenInclude зарежда VideoCard за всеки OrderItem (снимка и наименование).
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.User)         // Клиентът
                .Include(o => o.OrderItems)   // Елементите на поръчката
                    .ThenInclude(oi => oi.VideoCard)  // И за всеки елемент — видеокартата
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        /// <summary>
        /// POST: /Orders/UpdateStatus  (ADMIN)
        /// Обновява статуса на поръчка.
        /// Статуси: Pending → Processed → Shipped / Cancelled
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                order.Status = newStatus;
                await _context.SaveChangesAsync();
            }
            // Пренасочваме обратно към Details страницата за тази поръчка
            return RedirectToAction(nameof(Details), new { id = id });
        }

        /// <summary>
        /// GET: /Orders/Seed  (ADMIN) — Тестови метод
        /// Създава 2 тестови поръчки с различни статуси за демо цели.
        /// Изисква поне 1 потребител и 1 видеокарта в базата.
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Seed()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            var card = await _context.VideoCards.FirstOrDefaultAsync();

            if (user == null || card == null)
                return Content("Трябва първо да имаш регистриран потребител и добавена видеокарта!");

            // Поръчка 1: Статус Pending (нова)
            var testOrder2 = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                Status = "Изчакваща",
                TotalPrice = card.Price,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { VideoCardId = card.Id, Quantity = 1, PriceAtPurchase = card.Price }
                }
            };

            // Поръчка 2: Статус Cancelled (отказана) с 2 различни OrderItems
            var testOrder3 = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                Status = "Отказана",
                TotalPrice = card.Price,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { VideoCardId = card.Id, Quantity = 1, PriceAtPurchase = card.Price },
                    new OrderItem { VideoCardId = card.Id, Quantity = 2, PriceAtPurchase = card.Price }
                }
            };

            _context.Orders.Add(testOrder2);
            _context.Orders.Add(testOrder3);
            await _context.SaveChangesAsync();

            return Content("Тестовата поръчка е създадена! Отиди на /Orders/Index");
        }

        /// <summary>
        /// GET: /Orders/Checkout  (КЛИЕНТ)
        /// Показва преглед на количката преди финализиране на поръчката.
        /// Admin е блокиран — пренасочва се.
        /// Ако количката е празна — пренасочва към Cart/Index.
        /// </summary>
        public async Task<IActionResult> Checkout()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _context.CartItems
                .Include(c => c.VideoCard)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            // Ако количката е празна — няма смисъл от Checkout
            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            // Подаваме CartItems към Checkout.cshtml (не Order модел!)
            return View(cartItems);
        }

        /// <summary>
        /// POST: /Orders/ConfirmOrder  (КЛИЕНТ)
        /// ОСНОВНИЯТ МЕТОД ЗА ЗАВЪРШВАНЕ НА ПОРЪЧКА.
        ///
        /// Процес (в 1 транзакция):
        ///   1. Взима CartItems на потребителя
        ///   2. Създава Order с Status = "Pending"
        ///   3. Прехвърля CartItems → OrderItems (фиксира цените!)
        ///   4. Изтрива ВСИЧКИ CartItems на потребителя
        ///   5. Пренасочва към Success страницата
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Home");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // СТЪПКА 1: Вземаме всички CartItems на потребителя
            var cartItems = await _context.CartItems
                .Include(c => c.VideoCard)  // Нужен за Price при изчисляване на TotalPrice
                .Where(c => c.UserId == userId)
                .ToListAsync();

            // Ако количката е вече изпразнена — пренасочваме
            if (!cartItems.Any()) return RedirectToAction("UserIndex", "VideoCards");

            // СТЪПКА 2: Създаваме Order обекта.
            // TotalPrice = сума от (Quantity * цена) за всеки продукт
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "Изчакваща",  // Всяка нова поръчка е Изчакваща
                TotalPrice = cartItems.Sum(i => i.Quantity * i.VideoCard.Price),
                OrderItems = new List<OrderItem>()
            };

            // СТЪПКА 3: Прехвърляме CartItems → OrderItems.
            // КРИТИЧНО: PriceAtPurchase = i.VideoCard.Price в ТОЗИ МОМЕНТ.
            // Ако цената се промени след поръчката — историческата стойност остава.
            foreach (var item in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    VideoCardId = item.VideoCardId,
                    Quantity = item.Quantity,
                    PriceAtPurchase = item.VideoCard.Price  // Зафиксирана цена!
                });
            }

            _context.Orders.Add(order);

            // СТЪПКА 4: Изтриваме количката — тя вече е "конвертирана" в поръчка
            _context.CartItems.RemoveRange(cartItems);

            // Всичко горе в ЕДНА транзакция
            await _context.SaveChangesAsync();

            // СТЪПКА 5: Пренасочваме към Success с Id на поръчката
            return View("Success", order.Id);
        }

        /// <summary>
        /// GET: /Orders/MyOrders  (КЛИЕНТ)
        /// История на поръчките за текущия потребител.
        /// Admin се пренасочва към Index (всички поръчки).
        /// </summary>
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction(nameof(Index));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Зареждаме САМО поръчките на текущия потребител
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)  // Последните първо
                .ToListAsync();

            return View(orders);
        }
    }
}
