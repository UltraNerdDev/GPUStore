using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GPUStore.Data;
using GPUStore.Models;

namespace GPUStore.Controllers
{
    [Authorize] // Само логнати потребители имат достъп до количката
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Преглед на количката
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Home"); // Админът няма количка и го пренасочваме към началната страница
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Вземаме ID на текущия потребител

            var cartItems = await _context.CartItems
                .Include(c => c.VideoCard)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

        // 2. Добавяне в количката
        [HttpPost]
        public async Task<IActionResult> AddToCart(int cardId, int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Проверяваме дали този продукт вече е в количката на потребителя
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.VideoCardId == cardId && c.UserId == userId);

            if (existingItem == null)
            {
                existingItem = new CartItem
                {
                    VideoCardId = cardId,
                    UserId = userId,
                    Quantity = quantity // Използваме подадената бройка
                };
                _context.CartItems.Add(existingItem);
            }
            else
            {
                existingItem.Quantity += quantity; // Добавяме бройката към съществуващата
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // 3. Премахване от количката
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

        //[HttpPost]
        //public async Task<IActionResult> UpdateQuantity(int cardId, int change)
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var cartItem = await _context.CartItems
        //        .FirstOrDefaultAsync(c => c.VideoCardId == cardId && c.UserId == userId);

        //    if (cartItem != null)
        //    {
        //        cartItem.Quantity += change;

        //        // Ако количеството падне до 0 или по-малко, премахваме продукта
        //        if (cartItem.Quantity <= 0)
        //        {
        //            _context.CartItems.Remove(cartItem);
        //        }
        //        else
        //        {
        //            _context.Update(cartItem);
        //        }
        //        await _context.SaveChangesAsync();
        //    }

        //    return RedirectToAction("I
        //    ndex");
        //}
        [HttpPost]
        public async Task<IActionResult> UpdateQuantityAjax(int cardId, int change)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItem = await _context.CartItems
                .Include(c => c.VideoCard)
                .FirstOrDefaultAsync(c => c.VideoCardId == cardId && c.UserId == userId);

            if (cartItem == null) return Json(new { success = false });

            cartItem.Quantity += change;

            if (cartItem.Quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true, removed = true });
            }

            _context.Update(cartItem);
            await _context.SaveChangesAsync();

            // Преизчисляваме общата сума на количката
            var totalCartSum = await _context.CartItems
                .Where(c => c.UserId == userId)
                .SumAsync(c => c.Quantity * c.VideoCard.Price);

            return Json(new
            {
                success = true,
                newQuantity = cartItem.Quantity,
                itemTotal = (cartItem.Quantity * cartItem.VideoCard.Price).ToString("C2"),
                cartTotal = totalCartSum.ToString("C2")
            });
        }
    }
}