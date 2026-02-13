using GPUStore.Controllers;
using GPUStore.Data;
using GPUStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GPUStoreTests
{
    public class ManufacturersControllerTests
    {
        private static ApplicationDbContext CreateContextWithManufacturers(string dbName = null)
        {
            var name = dbName ?? Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: name)
                .Options;

            var context = new ApplicationDbContext(options);

            // Seed initial manufacturers if none exist
            if (!context.Manufacturers.Any())
            {
                context.Manufacturers.AddRange(
                    new Manufacturer { Id = 1, Name = "Nvidia" },
                    new Manufacturer { Id = 2, Name = "AMD" }
                );
                context.SaveChanges();
            }

            return context;
        }

        [Fact]
        public void Index_ReturnsViewResult_WithListOfManufacturers()
        {
            // Arrange
            using var context = CreateContextWithManufacturers();
            var controller = new ManufacturersController(context);

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<Manufacturer>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        [Fact]
        public async Task Edit_Get_ExistingId_ReturnsViewWithManufacturer()
        {
            // Arrange
            using var context = CreateContextWithManufacturers();
            var controller = new ManufacturersController(context);

            // Act
            var result = await controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Manufacturer>(viewResult.Model);
            Assert.Equal("Nvidia", model.Name);
        }

        [Fact]
        public async Task Edit_Get_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            using var context = CreateContextWithManufacturers();
            var controller = new ManufacturersController(context);

            // Act
            var result = await controller.Edit(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Post_ValidModel_CreatesManufacturerAndRedirects()
        {
            // Arrange
            // use a fresh DB without seeded manufacturers for clearer assertion
            using var context = CreateContextWithManufacturers(Guid.NewGuid().ToString());
            // remove seeded if present
            context.Manufacturers.RemoveRange(context.Manufacturers);
            context.SaveChanges();

            var controller = new ManufacturersController(context);
            var newManufacturer = new Manufacturer { Name = "Intel" };

            // Act
            var result = await controller.Create(newManufacturer);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ManufacturersController.Index), redirect.ActionName);

            var created = context.Manufacturers.FirstOrDefault(m => m.Name == "Intel");
            Assert.NotNull(created);
        }

        [Fact]
        public async Task DeleteConfirmed_DeletesManufacturerAndRedirects()
        {
            // Arrange
            using var context = CreateContextWithManufacturers();
            var controller = new ManufacturersController(context);

            // Act
            var result = await controller.DeleteConfirmed(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ManufacturersController.Index), redirect.ActionName);

            var deleted = context.Manufacturers.Find(1);
            Assert.Null(deleted);
        }
    }
}
