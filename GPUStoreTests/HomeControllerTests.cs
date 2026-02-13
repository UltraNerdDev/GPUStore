using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using GPUStore.Controllers;
using GPUStore.Data;
using GPUStore.Models;

namespace GPUStoreTests
{
    /// <summary>
    /// Unit tests for <see cref="HomeController"/>.
    /// Contains helpers that create an in-memory EF Core context and extract models from returned ViewResults.
    /// </summary>
    public class HomeControllerTests
    {
        /// <summary>
        /// Create a HomeController wired with an in-memory ApplicationDbContext.
        /// If <paramref name="seed"/> is true the context will be populated with a test manufacturer and two video cards.
        /// Ensures ControllerContext.HttpContext is not null to avoid NullReferenceException when controller code accesses it.
        /// </summary>
        /// <param name="seed">Whether to seed the in-memory database with sample data.</param>
        /// <returns>A HomeController instance ready for testing.</returns>
        private HomeController CreateController(bool seed = false)
        {
            // Build DbContext options using a unique in-memory database name so each test gets a fresh DB.
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);

            if (seed)
            {
                // Seed a manufacturer and two video cards for tests that require data.
                context.Manufacturers.Add(new Manufacturer { Id = 1, Name = "Test Manufacturer" });
                context.SaveChanges();

                context.VideoCards.AddRange(
                    new VideoCard { ModelName = "Test A", Price = 499.99m, ManufacturerId = 1 },
                    new VideoCard { ModelName = "Test B", Price = 299.99m, ManufacturerId = 1 }
                );
                context.SaveChanges();
            }

            // Use a null logger to avoid requiring a real logger in tests.
            var logger = new NullLogger<HomeController>();
            var controller = new HomeController(logger, context);

            // Ensure HttpContext is present to avoid NullReferenceException when controller actions access it (e.g., for Url, User, Request).
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        // Helper: try to extract a list of VideoCard objects from various possible model shapes.
        // The site may return a typed collection, an untyped collection (IEnumerable), or a wrapper object (e.g., a ViewModel).
        // This helper attempts multiple strategies to recover VideoCard-like objects for assertions.
        private List<VideoCard> ExtractVideoCards(object model)
        {
            if (model == null)
                return new List<VideoCard>();

            // If the model is already a typed IEnumerable<VideoCard>, just return it.
            if (model is IEnumerable<VideoCard> typedEnum)
                return typedEnum.ToList();

            // If model implements non-generic IEnumerable, iterate elements and try to map them to VideoCard instances.
            if (model is IEnumerable nonGeneric)
            {
                var list = new List<VideoCard>();
                foreach (var item in nonGeneric)
                {
                    if (item == null) continue;

                    // If the item is already a VideoCard instance, add directly.
                    if (item is VideoCard vc)
                    {
                        list.Add(vc);
                        continue;
                    }

                    // For anonymous objects or view-model types, reflectively look for ModelName and Price properties.
                    var t = item.GetType();
                    var modelNameProp = t.GetProperty("ModelName", BindingFlags.Public | BindingFlags.Instance);
                    var priceProp = t.GetProperty("Price", BindingFlags.Public | BindingFlags.Instance);
                    // Try to find an identifier property commonly used.
                    var idProp = t.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance) ?? t.GetProperty("VideoCardId", BindingFlags.Public | BindingFlags.Instance);

                    // If both ModelName and Price are present, attempt to read their values and construct a VideoCard.
                    if (modelNameProp != null && priceProp != null)
                    {
                        var modelName = modelNameProp.GetValue(item)?.ToString() ?? string.Empty;

                        decimal price = 0;
                        var priceObj = priceProp.GetValue(item);
                        if (priceObj is decimal d) price = d;
                        else if (priceObj != null) decimal.TryParse(priceObj.ToString(), out price);

                        int id = 0;
                        if (idProp != null)
                        {
                            var idObj = idProp.GetValue(item);
                            if (idObj != null) int.TryParse(idObj.ToString(), out id);
                        }

                        list.Add(new VideoCard { Id = id, ModelName = modelName, Price = price });
                    }
                }

                if (list.Count > 0) return list;
            }

            // If the model is a wrapper object (e.g., a view model with properties), scan its public instance properties recursively.
            var modelType = model.GetType();
            var props = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                // Skip indexer properties.
                if (prop.GetIndexParameters().Length > 0) continue;
                var val = prop.GetValue(model);
                if (val == null) continue;

                var extracted = ExtractVideoCards(val);
                if (extracted.Count > 0) return extracted;
            }

            // No VideoCard-like data found.
            return new List<VideoCard>();
        }
        

        [Fact]
        public async Task Index_ReturnsViewResult_WhenNoCards()
        {
            // Arrange: create controller with an empty database.
            var controller = CreateController(seed: false);

            // Act: call Index action which should return a ViewResult (possibly with an empty model).
            IActionResult actionResult = await controller.Index();

            // Assert: verify a ViewResult is returned and the model contains no video cards.
            var viewResult = Assert.IsType<ViewResult>(actionResult);
            var model = viewResult.Model;
            var cards = ExtractVideoCards(model);

            Assert.Empty(cards);
        }

        [Fact]
        public void Privacy_ReturnsViewResult()
        {
            // Arrange: create controller (seeding not necessary for this action).
            var controller = CreateController();

            // Act: call Privacy which returns a static ViewResult.
            IActionResult result = controller.Privacy();

            // Assert: result is a ViewResult.
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Error_ReturnsViewResult_WithRequestIdProperty()
        {
            // Arrange: create controller.
            var controller = CreateController();

            // Act: call Error which returns a ViewResult with an error view model.
            IActionResult result = controller.Error();

            // Assert: result is a ViewResult and its model exposes a readable RequestId property.
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = viewResult.Model;
            Assert.NotNull(model);

            // Ensure the model exposes a readable RequestId property (value may be null/empty in tests).
            var requestIdProp = model.GetType().GetProperty("RequestId", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(requestIdProp);
            Assert.True(requestIdProp.CanRead);
        }
    }
}
