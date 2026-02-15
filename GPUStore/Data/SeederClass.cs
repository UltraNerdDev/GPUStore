// ============================================================
// Data/SeederClass.cs — Начално зареждане на демо данни
// ============================================================
// SeederClass съдържа статичен метод Initialize(), който се извиква
// от HomeController при достъп до /Home/SeedDatabase (само Admin).
//
// Принцип: методът е ИДЕМПОТЕНТЕН — проверява дали данните вече
// съществуват преди да ги добавя. Може да се извика многократно
// без опасност от дублиране на записи.
// ============================================================

using GPUStore.Models;
using Microsoft.EntityFrameworkCore;

namespace GPUStore.Data
{
    public class SeederClass
    {
        /// <summary>
        /// Зарежда начални демо данни в базата данни.
        /// Извиква се само ако базата е празна (проверка за всяка таблица).
        /// </summary>
        /// <param name="serviceProvider">DI service provider за вземане на DbContext</param>
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            // Вземаме нов DbContext от DI контейнера.
            // using гарантира, че контекстът ще бъде Dispose-нат след метода.
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // ────────────────────────────────────────────────────────
            // СТЪПКА 1: Производители (Manufacturers)
            // ────────────────────────────────────────────────────────
            // Проверяваме дали таблицата е ПРАЗНА преди да добавяме.
            // Ако вече има производители — пропускаме тази стъпка изцяло.
            if (!context.Manufacturers.Any())
            {
                // Добавяме 10 производители: 5 GPU чипмейкъри и 5 партньорски марки.
                // NVIDIA и AMD са чипмейкърите; останалите са AIB партньори
                // (Add-In Board manufacturers), които правят конкретните карти.
                context.Manufacturers.AddRange(
                    new Manufacturer { Name = "NVIDIA" },    // Чипмейкър — RTX/GTX серии
                    new Manufacturer { Name = "AMD" },       // Чипмейкър — Radeon RX серии
                    new Manufacturer { Name = "ASUS" },      // AIB партньор — ROG, TUF, Dual серии
                    new Manufacturer { Name = "MSI" },       // AIB партньор — Gaming, Ventus, Suprim серии
                    new Manufacturer { Name = "Gigabyte" },  // AIB партньор — AORUS, Gaming OC серии
                    new Manufacturer { Name = "EVGA" },      // Само NVIDIA карти (вече прекрати бизнес с GPU)
                    new Manufacturer { Name = "Sapphire" },  // Само AMD карти — Pulse, Nitro+ серии
                    new Manufacturer { Name = "Zotac" },     // AIB партньор — Twin Edge, AMP серии
                    new Manufacturer { Name = "Palit" },     // AIB партньор — GameRock, Dual серии
                    new Manufacturer { Name = "PowerColor" } // Само AMD карти — Hellhound, Red Devil серии
                );
                await context.SaveChangesAsync();
            }

            // ────────────────────────────────────────────────────────
            // СТЪПКА 2: Технологии (Technologies)
            // ────────────────────────────────────────────────────────
            // GPU технологии, поддържани от модерните видеокарти.
            if (!context.Technologies.Any())
            {
                context.Technologies.AddRange(
                    new Technology { Name = "Ray Tracing" },  // Трасиране на лъчи — реалистично осветление (NVIDIA RTX)
                    new Technology { Name = "DLSS 3.0" },     // Deep Learning Super Sampling — AI upscaling от NVIDIA
                    new Technology { Name = "FSR 3.1" },      // FidelityFX Super Resolution — AMD open-source upscaling
                    new Technology { Name = "G-Sync" },       // NVIDIA adaptive sync за елиминиране на screen tearing
                    new Technology { Name = "FreeSync" },     // AMD adaptive sync (аналог на G-Sync)
                    new Technology { Name = "Reflex" },       // NVIDIA технология за намаляване на input latency
                    new Technology { Name = "Anti-Lag+" },    // AMD аналог на Reflex за намаляване на latency
                    new Technology { Name = "Resizable BAR" },// PCIe технология: CPU може да достъпва цялата VRAM
                    new Technology { Name = "VRS" },          // Variable Rate Shading — шейдиране с различна резолюция
                    new Technology { Name = "CUDA" }          // NVIDIA паралелни изчисления (не само за гейминг)
                );
                await context.SaveChangesAsync();
            }

            // ────────────────────────────────────────────────────────
            // СТЪПКА 3: Видеокарти (VideoCards)
            // ────────────────────────────────────────────────────────
            if (!context.VideoCards.Any())
            {
                // Вземаме Id-тата на производителите от базата след като са записани.
                // Използваме First() вместо директен Id, защото не знаем какви Id-та
                // ще генерира SQL Server (auto-increment).
                var nvidia = context.Manufacturers.First(m => m.Name == "NVIDIA").Id;
                var amd = context.Manufacturers.First(m => m.Name == "AMD").Id;
                var asus = context.Manufacturers.First(m => m.Name == "ASUS").Id;

                context.VideoCards.AddRange(
                    // ── NVIDIA Флагман ──
                    new VideoCard
                    {
                        ModelName = "GeForce RTX 4090",
                        Price = 3800.00m,         // Топ клас, Ada Lovelace архитектура
                        ManufacturerId = nvidia,  // Референтен дизайн от NVIDIA
                        Description = "Безспорният флагман на архитектурата Ada Lovelace...",
                        ImageUrl = "4090.jpg"
                    },
                    // ── AMD Флагман ──
                    new VideoCard
                    {
                        ModelName = "Radeon RX 7900 XTX",
                        Price = 2200.00m,   // RDNA 3 архитектура, чиплет дизайн
                        ManufacturerId = amd,
                        Description = "Най-мощното решение от AMD, базирано на RDNA 3 архитектурата...",
                        ImageUrl = "7900xtx.jpg"
                    },
                    // ── Висок клас (AIB партньор) ──
                    new VideoCard
                    {
                        ModelName = "ROG Strix RTX 4080 Super",
                        Price = 2600.00m,   // ASUS ROG (Republic of Gamers) премиум изпълнение
                        ManufacturerId = asus,
                        Description = "Премиум изпълнение от ASUS с масивно охлаждане...",
                        ImageUrl = "4080s.jpg"
                    },
                    // ── Горен среден клас ──
                    new VideoCard
                    {
                        ModelName = "GeForce RTX 4070 Ti",
                        Price = 1800.00m,   // Перфектен за 1440p гейминг
                        ManufacturerId = nvidia,
                        Description = "Перфектният избор за ултра-гейминг на 1440p резолюция...",
                        ImageUrl = "4070ti.jpg"
                    },
                    // ── Среден клас AMD ──
                    new VideoCard
                    {
                        ModelName = "Radeon RX 7800 XT",
                        Price = 1100.00m,   // 16GB VRAM — изключително за цената
                        ManufacturerId = amd,
                        Description = "Настоящият крал на средния клас...",
                        ImageUrl = "7800xt.jpg"
                    },
                    // ── Mainstream NVIDIA ──
                    new VideoCard
                    {
                        ModelName = "GeForce RTX 4060",
                        Price = 650.00m,   // Ефективен за 1080p, ниска консумация (115W)
                        ManufacturerId = nvidia,
                        Description = "Модерна видеокарта, фокусирана върху максималната енергийна ефективност...",
                        ImageUrl = "4060.jpg"
                    },
                    // ── Бюджетен AMD ──
                    new VideoCard
                    {
                        ModelName = "Radeon RX 7600",
                        Price = 580.00m,   // Идеален за esports заглавия
                        ManufacturerId = amd,
                        Description = "Достъпно и надеждно решение за популярни електронни спортове...",
                        ImageUrl = "7600.jpg"
                    },
                    // ── Предишно поколение (legacy) ──
                    new VideoCard
                    {
                        ModelName = "MSI Ventus RTX 3060",
                        Price = 600.00m,   // 12GB VRAM — голям за ценовия клас
                        ManufacturerId = nvidia,
                        Description = "Легендарен модел, който остава актуален благодарение на своите 12GB видео памет...",
                        ImageUrl = "3060.jpg"
                    },
                    // ── Среден клас AMD (предишно поколение) ──
                    new VideoCard
                    {
                        ModelName = "Sapphire Pulse RX 6700 XT",
                        Price = 750.00m,   // Компактен дизайн, Sapphire Pulse серия
                        ManufacturerId = amd,
                        Description = "Класика в средния клас от доказан партньор на AMD...",
                        ImageUrl = "6700xt.jpg"
                    },
                    // ── Бюджетен / Entry Level ──
                    new VideoCard
                    {
                        ModelName = "GTX 1650 Super",
                        Price = 300.00m,   // Без нужда от допълнително захранване (PCIe slot захранва)
                        ManufacturerId = nvidia,
                        Description = "Проверено във времето решение за ъпгрейд на по-стари офис конфигурации...",
                        ImageUrl = "1650.jpg"
                    }
                );
                await context.SaveChangesAsync();
            }

            // ────────────────────────────────────────────────────────
            // СТЪПКА 4: Релации CardTechnologies (VideoCard ↔ Technology)
            // ────────────────────────────────────────────────────────
            // Свързваме видеокартите с поддържаните от тях технологии.
            // Правим го само ако таблицата е напълно празна.
            if (!context.CardTechnologies.Any())
            {
                // Зареждаме всички карти и технологии в паметта,
                // за да можем да ги свържем по Id.
                var allCards = await context.VideoCards.ToListAsync();
                var allTechs = await context.Technologies.ToListAsync();

                var cardTechs = new List<CardTechnology>();

                foreach (var card in allCards)
                {
                    // Правило 1: Всички NVIDIA RTX карти поддържат Ray Tracing и DLSS 3.0.
                    // GTX 1650 не е RTX карта (не съдържа "RTX" в името), затова не получава тези технологии.
                    if (card.ModelName.Contains("RTX"))
                    {
                        var rt = allTechs.FirstOrDefault(t => t.Name == "Ray Tracing");
                        var dlss = allTechs.FirstOrDefault(t => t.Name == "DLSS 3.0");

                        if (rt != null) cardTechs.Add(new CardTechnology { VideoCardId = card.Id, TechnologyId = rt.Id });
                        if (dlss != null) cardTechs.Add(new CardTechnology { VideoCardId = card.Id, TechnologyId = dlss.Id });
                    }

                    // Правило 2: Всички AMD Radeon карти поддържат FSR 3.1.
                    // FSR е open-source и работи дори на NVIDIA карти,
                    // но тук го присвояваме само на Radeon.
                    if (card.ModelName.Contains("Radeon"))
                    {
                        var fsr = allTechs.FirstOrDefault(t => t.Name == "FSR 3.1");
                        if (fsr != null) cardTechs.Add(new CardTechnology { VideoCardId = card.Id, TechnologyId = fsr.Id });
                    }

                    // Правило 3: Всички карти поддържат Resizable BAR (ReBAR).
                    // Resizable BAR е PCIe стандарт — всяка модерна карта го поддържа.
                    var rebar = allTechs.FirstOrDefault(t => t.Name == "Resizable BAR");
                    if (rebar != null) cardTechs.Add(new CardTechnology { VideoCardId = card.Id, TechnologyId = rebar.Id });
                }

                context.CardTechnologies.AddRange(cardTechs);
                await context.SaveChangesAsync();
            }
        }
    }
}
