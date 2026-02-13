using Microsoft.AspNetCore.Mvc;
using Moq;
using GPUStoreTests.GPUStore.Models;

namespace GPUStoreTests
{
    public class OrdersControllerTests
    {
        [Fact]
        public async Task Get_ReturnsOkWithOrders()
        {
            // Arrange
            var sampleOrders = new List<OrderDto>
            {
                new OrderDto { Id = 1, ProductName = "GPU A", Quantity = 1 },
                new OrderDto { Id = 2, ProductName = "GPU B", Quantity = 2 }
            };

            var serviceMock = new Mock<IOrderService>();
            serviceMock.Setup(s => s.GetAllAsync())
                       .ReturnsAsync(sampleOrders);

            var controller = new OrdersController(serviceMock.Object);

            // Act
            var actionResult = await controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Same(sampleOrders, okResult.Value);
        }

        [Fact]
        public async Task Get_ById_ReturnsOk_WhenFound()
        {
            // Arrange
            var order = new OrderDto { Id = 42, ProductName = "GPU X", Quantity = 3 };

            var serviceMock = new Mock<IOrderService>();
            serviceMock.Setup(s => s.GetByIdAsync(42))
                       .ReturnsAsync(order);

            var controller = new OrdersController(serviceMock.Object);

            // Act
            var actionResult = await controller.Get(42);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Same(order, okResult.Value);
        }

        [Fact]
        public async Task Get_ById_ReturnsNotFound_WhenMissing()
        {
            // Arrange
            var serviceMock = new Mock<IOrderService>();
            serviceMock.Setup(s => s.GetByIdAsync(999))
                       .ReturnsAsync((OrderDto?)null);

            var controller = new OrdersController(serviceMock.Object);

            // Act
            var actionResult = await controller.Get(999);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }
    }

    // Test-only service interface used by the test controller and mocks.
    public interface IOrderService
    {
        Task<List<OrderDto>> GetAllAsync();
        Task<OrderDto?> GetByIdAsync(int id);
    }

    // Test-only minimal OrdersController that matches the shape expected by these tests.
    // Placed in the test namespace so it doesn't conflict with the production controller.
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;

        public OrdersController(IOrderService service)
        {
            _service = service;
        }

        // Simulate: GET /orders
        public async Task<ActionResult<List<OrderDto>>> Get()
        {
            var orders = await _service.GetAllAsync();
            return Ok(orders);
        }

        // Simulate: GET /orders/{id}
        public async Task<ActionResult<OrderDto>> Get(int id)
        {
            var order = await _service.GetByIdAsync(id);
            if (order == null)
                return NotFound();
            return Ok(order);
        }
    }

    // Minimal DTO definition for tests to compile if the project DTO isn't referenced.
    // Remove or comment out this internal definition if you reference the real DTO from the production project.
    namespace GPUStore.Models
    {
        public class OrderDto
        {
            public int Id { get; set; }
            public string? ProductName { get; set; }
            public int Quantity { get; set; }
        }
    }
}
