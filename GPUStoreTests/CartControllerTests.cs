using GPUStore.Controllers;
using GPUStore.Data;
using GPUStore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GPUStore.Models;

namespace GPUStoreTests
{
    public class CartControllerTests
    {
        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private CartController CreateControllerWithUser(ApplicationDbContext context, string userId, bool isAdmin = false)
        {
            var controller = new CartController(context);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }.ToList();

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }
        
        [Fact]
        public async Task Index_RedirectsToHome_ForAdmin()
        {
            // Verifies that an admin user trying to access the cart Index is redirected to the Home controller's Index action.
            // This ensures admins do not view regular user cart pages and are sent to the admin/home area.
            // Arrange
            using var context = CreateContext(nameof(Index_RedirectsToHome_ForAdmin));
            var controller = CreateControllerWithUser(context, userId: "admin-user", isAdmin: true);

            // Act
            var result = await controller.Index();

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }
        
        [Fact]
        public async Task Index_ReturnsCartItems_ForUser()
        {
            // Ensures that a regular (non-admin) user receives their cart items when calling Index.
            // Confirms the action returns a ViewResult whose model is an IEnumerable&lt;CartItem&gt; containing only the user's items.
            // Also validates the returned CartItem properties (UserId, VideoCardId, Quantity) match stored data.
            // Arrange
            using var context = CreateContext(nameof(Index_ReturnsCartItems_ForUser));


            var video = new VideoCard { Id = 1, ModelName = "GPU-1", Price = 100m, ImageUrl = "img.jpg" };
            context.VideoCards.Add(video);

            var cartItem = new CartItem
            {
                UserId = "user-1",
                VideoCardId = video.Id,
                Quantity = 2
            };
            context.CartItems.Add(cartItem);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, userId: "user-1");

            // Act
            var result = await controller.Index();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<CartItem>>(view.Model);
            Assert.Single(model);
            var item = model.First();
            Assert.Equal("user-1", item.UserId);
            Assert.Equal(video.Id, item.VideoCardId);
            Assert.Equal(2, item.Quantity);
        }      
        [Fact]
        public async Task AddToCart_AddsNewItem_WhenNotExisting()
        {
            // Tests that calling AddToCart for a VideoCard that the user doesn't already have in their cart
            // creates a new CartItem with Quantity = 1 and that the action returns a redirect result.
            // Arrange
            using var context = CreateContext(nameof(AddToCart_AddsNewItem_WhenNotExisting));

            var video = new VideoCard { Id = 10, ModelName = "GPU-10", Price = 250m, ImageUrl = "img10.jpg" };
            context.VideoCards.Add(video);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, userId: "buyer-1");

            // Act
            var result = await controller.AddToCart(video.Id);

            // Assert redirect
            Assert.IsType<RedirectToActionResult>(result);
            // Verify item added
            var added = context.CartItems.FirstOrDefault(ci => ci.UserId == "buyer-1" && ci.VideoCardId == video.Id);
            Assert.NotNull(added);
            Assert.Equal(1, added.Quantity);
        }
        [Fact]
        public async Task Remove_RemovesItem_FromCart()
        {
            // Verifies that Remove deletes the specified CartItem for the current user and that the action returns a redirect.
            // After calling Remove the CartItem should no longer exist in the database.
            // Arrange
            using var context = CreateContext(nameof(Remove_RemovesItem_FromCart));

            var item = new CartItem
            {
                UserId = "user-to-remove",
                VideoCardId = 5,
                Quantity = 1
            };
            context.CartItems.Add(item);
            await context.SaveChangesAsync();

            var controller = CreateControllerWithUser(context, userId: "user-to-remove");

            // Act
            var result = await controller.Remove(item.Id);

            // Assert redirect
            Assert.IsType<RedirectToActionResult>(result);
            // Verify removal
            var exists = context.CartItems.Any(ci => ci.Id == item.Id);
            Assert.False(exists);
        }
    }
}
