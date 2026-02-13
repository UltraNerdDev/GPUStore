using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using GPUStore.Controllers;
using GPUStore.Models;
using GPUStore.Data;

namespace GPUStoreTests
{
    public class TechnologiesControllerTests
    {
        private ApplicationDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private void SeedTechnologies(ApplicationDbContext ctx, params Technology[] techs)
        {
            ctx.Technologies.AddRange(techs);
            ctx.SaveChanges();
        }

        [Fact]
        public void Index_Returns_View_With_All_Technologies()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateDbContext(dbName);
            var t1 = new Technology { Id = 1, Name = "TechA" };
            var t2 = new Technology { Id = 2, Name = "TechB" };
            SeedTechnologies(ctx, t1, t2);

            var controller = new TechnologiesController(ctx);

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Technology>>(viewResult.Model);
            Assert.Equal(2, model.Count());
            Assert.Collection(model,
                item => Assert.Equal("TechA", item.Name),
                item => Assert.Equal("TechB", item.Name));
        }

        

        [Fact]
        public async Task Create_Post_Valid_Redirects_To_Index_And_Adds_Technology()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateDbContext(dbName);

            var controller = new TechnologiesController(ctx);
            var input = new Technology { Name = "DLSS" };

            // Act
            var result = await controller.Create(input);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            // Verify DB contains the created technology
            var stored = ctx.Technologies.FirstOrDefault(t => t.Name == "DLSS");
            Assert.NotNull(stored);
        }

        [Fact]
        public async Task Edit_Get_Returns_View_With_Technology()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateDbContext(dbName);
            var tech = new Technology { Id = 7, Name = "Original" };
            SeedTechnologies(ctx, tech);

            var controller = new TechnologiesController(ctx);

            // Act
            var result = await controller.Edit(7);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Technology>(viewResult.Model);
            Assert.Equal(7, model.Id);
            Assert.Equal("Original", model.Name);
        }

        

        [Fact]
        public async Task Delete_Get_Returns_View_With_Technology()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateDbContext(dbName);
            var tech = new Technology { Id = 9, Name = "ToDelete" };
            SeedTechnologies(ctx, tech);

            var controller = new TechnologiesController(ctx);

            // Act
            var result = await controller.Delete(9);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Technology>(viewResult.Model);
            Assert.Equal(9, model.Id);
            Assert.Equal("ToDelete", model.Name);
        }

        [Fact]
        public async Task DeleteConfirmed_Removes_And_Redirects_To_Index()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var ctx = CreateDbContext(dbName);
            var tech = new Technology { Id = 99, Name = "Removable" };
            SeedTechnologies(ctx, tech);

            var controller = new TechnologiesController(ctx);

            // Act
            var result = await controller.DeleteConfirmed(99);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var exists = ctx.Technologies.Any(t => t.Id == 99);
            Assert.False(exists);
        }
    }
}
