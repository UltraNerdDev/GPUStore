using GPUStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Data
{
    public class SeederClass
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // 1. Проверка за Производители
            if (!context.Manufacturers.Any())
            {
                context.Manufacturers.AddRange(
                    new Manufacturer { Name = "NVIDIA" },
                    new Manufacturer { Name = "AMD" },
                    new Manufacturer { Name = "ASUS" },
                    new Manufacturer { Name = "MSI" },
                    new Manufacturer { Name = "Gigabyte" },
                    new Manufacturer { Name = "EVGA" },
                    new Manufacturer { Name = "Sapphire" },
                    new Manufacturer { Name = "Zotac" },
                    new Manufacturer { Name = "Palit" },
                    new Manufacturer { Name = "PowerColor" }
                );
                await context.SaveChangesAsync();
            }

            // 2. Проверка за Технологии
            if (!context.Technologies.Any())
            {
                context.Technologies.AddRange(
                    new Technology { Name = "Ray Tracing" },
                    new Technology { Name = "DLSS 3.0" },
                    new Technology { Name = "FSR 3.1" },
                    new Technology { Name = "G-Sync" },
                    new Technology { Name = "FreeSync" },
                    new Technology { Name = "Reflex" },
                    new Technology { Name = "Anti-Lag+" },
                    new Technology { Name = "Resizable BAR" },
                    new Technology { Name = "VRS" },
                    new Technology { Name = "CUDA" }
                );
                await context.SaveChangesAsync();
            }

            // 3. Проверка за Видеокарти
            if (!context.VideoCards.Any())
            {
                var nvidia = context.Manufacturers.First(m => m.Name == "NVIDIA").Id;
                var amd = context.Manufacturers.First(m => m.Name == "AMD").Id;
                var asus = context.Manufacturers.First(m => m.Name == "ASUS").Id;

                context.VideoCards.AddRange(
                    new VideoCard { ModelName = "GeForce RTX 4090", Price = 3800.00m, ManufacturerId = nvidia, Description = "Флагманът на NVIDIA.", ImageUrl = "4090.jpg" },
                    new VideoCard { ModelName = "Radeon RX 7900 XTX", Price = 2200.00m, ManufacturerId = amd, Description = "Най-бързата карта на AMD.", ImageUrl = "7900xtx.jpg" },
                    new VideoCard { ModelName = "ROG Strix RTX 4080 Super", Price = 2600.00m, ManufacturerId = asus, Description = "Премиум изпълнение от ASUS.", ImageUrl = "4080s.jpg" },
                    new VideoCard { ModelName = "GeForce RTX 4070 Ti", Price = 1800.00m, ManufacturerId = nvidia, Description = "Перфектна за 1440p.", ImageUrl = "4070ti.jpg" },
                    new VideoCard { ModelName = "Radeon RX 7800 XT", Price = 1100.00m, ManufacturerId = amd, Description = "Кралят на средния клас.", ImageUrl = "7800xt.jpg" },
                    new VideoCard { ModelName = "GeForce RTX 4060", Price = 650.00m, ManufacturerId = nvidia, Description = "Ефективност и бюджет.", ImageUrl = "4060.jpg" },
                    new VideoCard { ModelName = "Radeon RX 7600", Price = 580.00m, ManufacturerId = amd, Description = "Бюджетно решение за 1080p.", ImageUrl = "7600.jpg" },
                    new VideoCard { ModelName = "MSI Ventus RTX 3060", Price = 600.00m, ManufacturerId = nvidia, Description = "Легендарна карта с 12GB VRAM.", ImageUrl = "3060.jpg" },
                    new VideoCard { ModelName = "Sapphire Pulse RX 6700 XT", Price = 750.00m, ManufacturerId = amd, Description = "Класика в средния клас.", ImageUrl = "6700xt.jpg" },
                    new VideoCard { ModelName = "GTX 1650 Super", Price = 300.00m, ManufacturerId = nvidia, Description = "За стари системи и леки игри.", ImageUrl = "1650.jpg" }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
