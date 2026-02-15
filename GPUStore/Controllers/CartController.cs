// ============================================================
// Controllers/CartController.cs — Пазарска количка
// ============================================================
// Управлява временното съхранение на продукти преди поръчката.
// Количката е персонална — всеки потребител вижда само своите продукти.
// Admin изобщо НЯМА количка — пренасочва се към Home при опит за достъп.
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GPUStore.Data;
using GPUStore.Models;

namespace GPUStore.Controllers
{
    // [Authorize] — само логнати потребители имат количка
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// GET: /Cart
        /// Показва всички продукти в количката на текущия потребител.
        /// Admin се пренасочва — той не ползва количка.
        public async Task<IActionResult> Index()
        {
            // Защита: Admin няма смисъл да гледа количка
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Home");
            }

            // ClaimTypes.NameIdentifier = уникалното GUID Id на потребителя от AspNetUsers.
            // Всеки Identity потребител го има.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Зареждаме CartItems САМО на текущия потребител.
            // Include(c => c.VideoCard) — нужно е за показване на имена и снимки.
            var cartItems = await _context.CartItems
                .Include(c => c.VideoCard)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

        /// POST: /Cart/AddToCart
        /// Добавя продукт в количката или увеличава количеството.
        ///
        /// Логика: Ако картата ВЕЧЕe в количката — увеличаваме Quantity.
        ///         Ако НЕ е — създаваме нов CartItem запис.
        [HttpPost]
        public async Task<IActionResult> AddToCart(int cardId, int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Търсим съществуващ CartItem за ТОЗИ потребител И ТАЗИ карта
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.VideoCardId == cardId && c.UserId == userId);

            if (existingItem == null)
            {
                // Продуктът не е в количката — създаваме нов запис
                existingItem = new CartItem
                {
                    VideoCardId = cardId,
                    UserId = userId,
                    Quantity = quantity
                };
                _context.CartItems.Add(existingItem);
            }
            else
            {
                // Продуктът ВЕЧЕe в количката — ДОБАВЯМЕ към наличното количество
                // (не заменяме!)
                existingItem.Quantity += quantity;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// GET: /Cart/Remove/5
        /// Изтрива CartItem от количката и пренасочва обратно.
        /// Параметърът е CartItem.Id (не VideoCard.Id).
        public async Task<IActionResult> Remove(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        /// POST: /Cart/UpdateQuantityAjax
        /// AJAX метод за промяна на количество без презареждане на страницата.
        /// Приема change = +1 (увеличаване) или -1 (намаляване).
        /// Връща JSON резултат за обновяване на DOM от JavaScript.
        [HttpPost]
        public async Task<IActionResult> UpdateQuantityAjax(int cardId, int change)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Зареждаме CartItem заедно с VideoCard (нужен е за изчисляване на цената)
            var cartItem = await _context.CartItems
                .Include(c => c.VideoCard)
                .FirstOrDefaultAsync(c => c.VideoCardId == cardId && c.UserId == userId);

            // Ако не намерим елемента — JSON { success: false }
            if (cartItem == null) return Json(new { success = false });

            cartItem.Quantity += change;

            // Ако количеството падне до 0 или по-малко — изтриваме елемента
            if (cartItem.Quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                // Казваме на JavaScript да презареди страницата (редът изчезва)
                return Json(new { success = true, removed = true });
            }

            _context.Update(cartItem);
            await _context.SaveChangesAsync();

            // Преизчисляваме ОБЩАТА сума на ЦЯЛАТА количка след промяната.
            // Нужно е за обновяване на "Обща сума" в footer-а на таблицата.
            var totalCartSum = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity * c.VideoCard.Price);

            // Връщаме актуализираните стойности като JSON.
            // JavaScript ги чете и обновява DOM елементите без reload.
            return Json(new
            {
                success = true,
                newQuantity = cartItem.Quantity,
                // "C2" формат = валута с 2 десетични места (напр. "1 200,00 лв.")
                itemTotal = (cartItem.Quantity * cartItem.VideoCard.Price).ToString("C2"),
                cartTotal = totalCartSum.ToString("C2")
            });
        }
    }
}
