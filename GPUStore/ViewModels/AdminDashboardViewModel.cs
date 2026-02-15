// ============================================================
// ViewModels/AdminDashboardViewModel.cs
// ============================================================
// Пренася обобщена статистика към AdminIndex.cshtml Dashboard.
// Попълва се в HomeController.Index() чрез 4 async DB заявки.
// ============================================================

namespace GPUStore.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Брой видеокарти от VideoCards.CountAsync()
        public int TotalVideoCards { get; set; }

        // Брой поръчки от Orders.CountAsync()
        public int TotalOrders { get; set; }

        // Брой производители от Manufacturers.CountAsync()
        public int TotalManufacturers { get; set; }

        // Общ оборот = сума на TotalPrice на всички поръчки
        // Изчислено с Orders.SumAsync(o => o.TotalPrice)
        public decimal TotalRevenue { get; set; }
    }
}