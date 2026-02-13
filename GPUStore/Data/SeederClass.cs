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
                new VideoCard
                {
                    ModelName = "GeForce RTX 4090",
                    Price = 3800.00m,
                    ManufacturerId = nvidia,
                    Description = "Безспорният флагман на архитектурата Ada Lovelace. RTX 4090 предлага ненадмината мощност за 4K гейминг при максимални настройки и професионална работа с 3D графика. Разполага с 24GB GDDR6X памет и поддържа най-новите технологии за изкуствен интелект и Ray Tracing.",
                    ImageUrl = "4090.jpg"
                },
                new VideoCard
                {
                    ModelName = "Radeon RX 7900 XTX",
                    Price = 2200.00m,
                    ManufacturerId = amd,
                    Description = "Най-мощното решение от AMD, базирано на RDNA 3 архитектурата. С 24GB VRAM и иновативен чиплет дизайн, тази карта е създадена за геймъри, които търсят висока производителност в 4K и отлична поддръжка на софтуер с отворен код. Изключително съотношение между цена и мощност.",
                    ImageUrl = "7900xtx.jpg"
                },
                new VideoCard
                {
                    ModelName = "ROG Strix RTX 4080 Super",
                    Price = 2600.00m,
                    ManufacturerId = asus,
                    Description = "Премиум изпълнение от ASUS с масивно охлаждане и агресивен RGB дизайн. Тази карта е оптимизирана за изключително ниски работни температури и минимален шум. Идеална за ентусиасти, които искат най-доброто качество на компонентите и висок потенциал за овърклок.",
                    ImageUrl = "4080s.jpg"
                },
                new VideoCard
                {
                    ModelName = "GeForce RTX 4070 Ti",
                    Price = 1800.00m,
                    ManufacturerId = nvidia,
                    Description = "Перфектният избор за ултра-гейминг на 1440p резолюция. RTX 4070 Ti предлага баланс между висока кадрова честота и енергийна ефективност. Поддържа DLSS 3 Frame Generation, което позволява плавна игра дори в най-тежките заглавия с включен Ray Tracing.",
                    ImageUrl = "4070ti.jpg"
                },
                new VideoCard
                {
                    ModelName = "Radeon RX 7800 XT",
                    Price = 1100.00m,
                    ManufacturerId = amd,
                    Description = "Настоящият крал на средния клас. Със своите 16GB памет, RX 7800 XT е подготвена за бъдещите игри, изискващи голям обем VRAM. Предлага отлична производителност в 1440p и поддържа технологията FSR 3 за допълнително ускорение на кадрите.",
                    ImageUrl = "7800xt.jpg"
                },
                new VideoCard
                {
                    ModelName = "GeForce RTX 4060",
                    Price = 650.00m,
                    ManufacturerId = nvidia,
                    Description = "Модерна видеокарта, фокусирана върху максималната енергийна ефективност. Идеална за компактни системи и гейминг на 1080p. Въпреки ниската си консумация, тя предлага достъп до пълния пакет технологии на NVIDIA, включително DLSS 3.0 и Reflex.",
                    ImageUrl = "4060.jpg"
                },
                new VideoCard
                {
                    ModelName = "Radeon RX 7600",
                    Price = 580.00m,
                    ManufacturerId = amd,
                    Description = "Достъпно и надеждно решение за популярни електронни спортове и гейминг на 1080p. RX 7600 е създадена за потребители, които искат модерна архитектура и висока скорост в заглавия като CS2, Valorant и LoL, без да натоварват бюджета си излишно.",
                    ImageUrl = "7600.jpg"
                },
                new VideoCard
                {
                    ModelName = "MSI Ventus RTX 3060",
                    Price = 600.00m,
                    ManufacturerId = nvidia,
                    Description = "Легендарен модел, който остава актуален благодарение на своите 12GB видео памет. Ventus серията на MSI предлага изчистен дизайн и стабилна работа. Страхотен избор за геймъри, които търсят сигурна производителност и дълъг живот на хардуера.",
                    ImageUrl = "3060.jpg"
                },
                new VideoCard
                {
                    ModelName = "Sapphire Pulse RX 6700 XT",
                    Price = 750.00m,
                    ManufacturerId = amd,
                    Description = "Класика в средния клас от доказан партньор на AMD. Sapphire Pulse се отличава с компактен дизайн и изключително надеждно охлаждане. Картата е способна да се справи с всяка игра на високи настройки, предлагайки чиста мощност без излишни екстри.",
                    ImageUrl = "6700xt.jpg"
                },
                new VideoCard
                {
                    ModelName = "GTX 1650 Super",
                    Price = 300.00m,
                    ManufacturerId = nvidia,
                    Description = "Проверено във времето решение за ъпгрейд на по-стари офис конфигурации или бюджетни геймърски машини. Идеална за леки игри и мултимедия. Не изисква мощно захранване, което я прави лесна за инсталиране във всяка кутия.",
                    ImageUrl = "1650.jpg"
                }
                );
                await context.SaveChangesAsync();
            }

            // 4. Проверка за релации (CardTechnologies)
            if (!context.CardTechnologies.Any())
            {
                // Вземаме всички карти и технологии от базата, за да им знаем ID-тата
                var allCards = await context.VideoCards.ToListAsync();
                var allTechs = await context.Technologies.ToListAsync();

                var cardTechs = new List<CardTechnology>();

                foreach (var card in allCards)
                {
                    // Примерен алгоритъм за сийдване:

                    // 1. Всички NVIDIA карти (RTX) получават Ray Tracing и DLSS
                    if (card.ModelName.Contains("RTX"))
                    {
                        var rt = allTechs.FirstOrDefault(t => t.Name == "Ray Tracing");
                        var dlss = allTechs.FirstOrDefault(t => t.Name == "DLSS 3.0");

                        if (rt != null) cardTechs.Add(new CardTechnology { VideoCardId = card.Id, TechnologyId = rt.Id });
                        if (dlss != null) cardTechs.Add(new CardTechnology { VideoCardId = card.Id, TechnologyId = dlss.Id });
                    }

                    // 2. Всички AMD карти получават FSR
                    if (card.ModelName.Contains("Radeon"))
                    {
                        var fsr = allTechs.FirstOrDefault(t => t.Name == "FSR 3.1");
                        if (fsr != null) cardTechs.Add(new CardTechnology { VideoCardId = card.Id, TechnologyId = fsr.Id });
                    }

                    // 3. Всички карти получават Resizable BAR
                    var rebar = allTechs.FirstOrDefault(t => t.Name == "Resizable BAR");
                    if (rebar != null) cardTechs.Add(new CardTechnology { VideoCardId = card.Id, TechnologyId = rebar.Id });
                }

                context.CardTechnologies.AddRange(cardTechs);
                await context.SaveChangesAsync();
            }
        }
    }
}
