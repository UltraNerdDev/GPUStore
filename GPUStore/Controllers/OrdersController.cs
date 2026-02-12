using GPUStore.Data;
using GPUStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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

        // 1. Списък с всички поръчки
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.User) // Да видим кой е поръчал
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        // 2. Детайли на конкретна поръчка
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.VideoCard) // Да видим кои карти са вътре
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // 3. Промяна на статус (напр. от "Обработва се" на "Изпратена")
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
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // ВРЕМЕНЕН МЕТОД: Извикай /Orders/Seed в браузъра
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Seed()
        {
            // Вземаме първия потребител и първата видеокарта от базата
            var user = await _context.Users.FirstOrDefaultAsync();
            var card = await _context.VideoCards.FirstOrDefaultAsync();

            if (user == null || card == null)
                return Content("Трябва първо да имаш регистриран потребител и добавена видеокарта!");

            var testOrder2 = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                Status = "Pending",
                TotalPrice = card.Price,
                OrderItems = new List<OrderItem>
            {
                    new OrderItem
                    {
                        VideoCardId = card.Id,
                        Quantity = 1,
                        PriceAtPurchase = card.Price
                    }
                }
            };

            var testOrder3 = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                Status = "Cancelled",
                TotalPrice = card.Price,
                OrderItems = new List<OrderItem>
            {
                    new OrderItem
                    {
                        VideoCardId = card.Id,
                        Quantity = 1,
                        PriceAtPurchase = card.Price
                    },
                    new OrderItem
                    {
                        VideoCardId = card.Id,
                        Quantity = 2,
                        PriceAtPurchase = card.Price
                    }
                }
            };

            _context.Orders.Add(testOrder2);
            _context.Orders.Add(testOrder3);
            await _context.SaveChangesAsync();

            return Content("Тестовата поръчка е създадена! Отиди на /Orders/Index");
        }

        public async Task<IActionResult> Checkout()
        {
            // ЗАЩИТА: Ако админът се опита да влезе ръчно, го гоним
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Home");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Вземаме продуктите от количката
            var cartItems = await _context.CartItems
                .Include(c => c.VideoCard)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            return View(cartItems);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder()
        {
            // ЗАЩИТА: Ако админът се опита да влезе ръчно, го гоним
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Home");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Вземаме продуктите от количката
            var cartItems = await _context.CartItems
                .Include(c => c.VideoCard)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("UserIndex", "VideoCards");

            // 2. Създаваме основната поръчка
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = "Pending",
                TotalPrice = cartItems.Sum(i => i.Quantity * i.VideoCard.Price),
                OrderItems = new List<OrderItem>()
            };

            // 3. Прехвърляме продуктите от количката в OrderItems
            foreach (var item in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    VideoCardId = item.VideoCardId,
                    Quantity = item.Quantity,
                    PriceAtPurchase = item.VideoCard.Price // Запазваме цената в момента на покупката
                });
            }

            _context.Orders.Add(order);

            // 4. ИЗТРИВАМЕ количката на потребителя
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            // 5. Препращаме към страница за успех
            return View("Success", order.Id);
        }

        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            // ЗАЩИТА: Ако е админ, го пращаме към списъка за управление на всички поръчки
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction(nameof(Index));
            }

            // Вземаме ID-то на текущия потребител
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Извличаме само неговите поръчки, подредени от най-новите към най-старите
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }
    }
}
