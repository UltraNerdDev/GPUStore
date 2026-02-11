using System.Diagnostics;
using GPUStore.Data;
using GPUStore.Models;
using GPUStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Ако потребителят е Админ - подготвяме статистиката и му даваме неговото View
            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
            {
                var stats = new AdminDashboardViewModel
                {
                    TotalVideoCards = await _context.VideoCards.CountAsync(),
                    TotalOrders = await _context.Orders.CountAsync(),
                    TotalManufacturers = await _context.Manufacturers.CountAsync(),
                    TotalRevenue = await _context.Orders.SumAsync(o => o.TotalPrice)
                };

                return View("AdminIndex", stats); // Ще ползваме отделно View файл за по-чист код
            }

            // Ако е обикновен потребител или не е логнат - показваме стандартната начална страница
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
