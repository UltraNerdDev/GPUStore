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
        public async Task<IActionResult> AddToCart(int cardId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Проверяваме дали този продукт вече е в количката на потребителя
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.VideoCardId == cardId && c.UserId == userId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                var cartItem = new CartItem
                {
                    UserId = userId,
                    VideoCardId = cardId,
                    Quantity = 1
                };
                _context.CartItems.Add(cartItem);
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
    }
}