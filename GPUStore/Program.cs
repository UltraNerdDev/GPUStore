using GPUStore.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GPUStore
{
    public class Program
    {
        // Main е async, защото при стартиране се правят async операции
        // (създаване на роли и admin потребител спрямо базата данни)
        public static async Task Main(string[] args)
        {
            // ────────────────────────────────────────────────────────
            // СТЪПКА 1: Създаване на WebApplication builder
            // ────────────────────────────────────────────────────────
            // builder е "строителят" на приложението — чрез него се
            // регистрират всички услуги (services) в DI контейнера.
            var builder = WebApplication.CreateBuilder(args);

            // ────────────────────────────────────────────────────────
            // СТЪПКА 2: Регистрация на базата данни (EF Core + SQL Server)
            // ────────────────────────────────────────────────────────
            // Четем connection string от appsettings.json.
            // Ако не съществува — хвърля изключение веднага (fail-fast),
            // вместо да гърми по-късно при първа заявка.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // Регистрираме ApplicationDbContext като scoped услуга.
            // UseSqlServer казва на EF Core кой SQL провайдер да използва.
            // ApplicationDbContext е нашият DbContext, наследяващ IdentityDbContext.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Добавя middleware за показване на подробни EF Core грешки
            // само в Development среда (миграции, schema проблеми и др.)
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // ────────────────────────────────────────────────────────
            // СТЪПКА 3: Регистрация на ASP.NET Core Identity
            // ────────────────────────────────────────────────────────
            // AddDefaultIdentity<IdentityUser> — стандартна Identity конфигурация
            // с вграден IdentityUser модел (без custom потребителски клас).
            //
            // options.SignIn.RequireConfirmedAccount = false — позволява вход
            // без потвърждаване на имейл адреса. В Production трябва да е true.
            //
            // .AddRoles<IdentityRole>() — КРИТИЧНО: добавя ролевата система.
            // Без това нямаме RoleManager и [Authorize(Roles = "Admin")] не работи.
            //
            // .AddEntityFrameworkStores<ApplicationDbContext>() — казва на Identity
            // да съхранява потребителите и ролите в нашата SQL база (ApplicationDbContext).
            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
                options.SignIn.RequireConfirmedAccount = false)
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<ApplicationDbContext>();

            // Регистрира MVC контролерите и Razor изгледите.
            // Тук се добавят и TempData, ModelState, validation и др. MVC услуги.
            builder.Services.AddControllersWithViews();

            // ────────────────────────────────────────────────────────
            // СТЪПКА 4: Изграждане на приложението
            // ────────────────────────────────────────────────────────
            // След Build() вече не можем да регистрираме нови услуги.
            // Тук преминаваме от конфигурация към изпълнение.
            var app = builder.Build();

            // ────────────────────────────────────────────────────────
            // СТЪПКА 5: Конфигурация на HTTP middleware pipeline
            // ────────────────────────────────────────────────────────
            // Редът на middleware-ите е КРИТИЧЕН — всяка заявка преминава
            // през тях в точно този ред от горе надолу.

            if (app.Environment.IsDevelopment())
            {
                // В Development среда: показва подробна страница за EF Core грешки
                // (полезно при проблеми с миграциите)
                app.UseMigrationsEndPoint();
            }
            else
            {
                // В Production среда: показва generic страница за грешки
                // вместо техническа информация (по-сигурно за потребителите)
                app.UseExceptionHandler("/Home/Error");

                // HSTS (HTTP Strict Transport Security): казва на браузъра
                // да комуникира само по HTTPS за следващите 30 дни.
                app.UseHsts();
            }

            // Пренасочва HTTP заявките към HTTPS (напр. http://... → https://...)
            app.UseHttpsRedirection();

            // Конфигурира URL маршрутизирането.
            // ТРЯБВА да е преди UseAuthentication/UseAuthorization.
            app.UseRouting();

            // ВАЖЕН РЕД: UseAuthentication ПРЕДИ UseAuthorization!
            // UseAuthentication: чете cookies/tokens и зарежда User обекта
            // UseAuthorization: проверява дали заредения User има право на достъп
            // Ако разменим реда — потребителят ще изглежда неаутентикиран
            // дори когато е влязъл в системата.
            app.UseAuthentication();
            app.UseAuthorization();

            // Обслужва статичните файлове от wwwroot/ (CSS, JS, изображения)
            // MapStaticAssets е оптимизираната версия в .NET 9
            app.MapStaticAssets();

            // Маршрутизира заявките към MVC контролерите.
            // Шаблонът {controller=Home}/{action=Index}/{id?} означава:
            // - По подразбиране: HomeController.Index()
            // - id е незадължителен параметър
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            // Маршрутизира Identity Razor Pages (/Identity/Account/Login и др.)
            app.MapRazorPages()
               .WithStaticAssets();

            // ────────────────────────────────────────────────────────
            // СТЪПКА 6: Автоматично seed на роли и Admin потребител
            // ────────────────────────────────────────────────────────
            // CreateScope() създава временен DI scope, за да вземем
            // scoped услуги (RoleManager, UserManager) извън HTTP заявка.
            // using гарантира, че scope-ът ще бъде изчистен след блока.
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                // --- Стъпка 6а: Създаване на ролите ---
                // Дефинираме двете роли в системата.
                // RoleExistsAsync() предотвратява дублиране при всяко рестартиране.
                string[] roleNames = { "Admin", "Customer" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // --- Стъпка 6б: Създаване на Admin потребител ---
                // Проверяваме дали admin@gpustore.com вече съществува.
                // Ако не — създаваме го с фиксирана парола и го добавяме в Admin роля.
                // ВНИМАНИЕ: Паролата "Admin123!" е хардкодирана само за демо цели!
                // В реална система трябва да се чете от Environment Variables или Secret Manager.
                var adminEmail = "admin@gpustore.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    var user = new IdentityUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true // Предварително потвърден имейл
                    };
                    await userManager.CreateAsync(user, "Admin123!");
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }

            // Стартира приложението и започва слушане на HTTP заявки
            await app.RunAsync();
        }
    }
}
