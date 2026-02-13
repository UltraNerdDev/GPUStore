using GPUStore.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GPUStoreTests
{
    // Repository interface (kept here for test compilation)
    public interface IVideoCardRepository
    {
        IEnumerable<VideoCard> GetAll();
        VideoCard? GetById(int id);
        void Add(VideoCard card);
        bool Update(VideoCard card);
        bool Delete(int id);
        IEnumerable<VideoCard> FindByManufacturer(string manufacturer);
    }

    // Minimal API-style controller to be tested (kept simple for unit tests)
    [ApiController]
    [Route("api/[controller]")]
    public class VideoCardsController : ControllerBase
    {
        private readonly IVideoCardRepository _repo;

        public VideoCardsController(IVideoCardRepository repo) => _repo = repo;

        [HttpGet]
        public ActionResult<IEnumerable<VideoCard>> Get() => Ok(_repo.GetAll());

        [HttpGet("{id}")]
        public ActionResult<VideoCard> Get(int id)
        {
            var item = _repo.GetById(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public ActionResult<VideoCard> Post(VideoCard card)
        {
            if (_repo.GetById(card.Id) != null) return BadRequest("Item with same Id already exists.");
            _repo.Add(card);
            return CreatedAtAction(nameof(Get), new { id = card.Id }, card);
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, VideoCard card)
        {
            if (id != card.Id) return BadRequest("Id mismatch.");
            if (!_repo.Update(card)) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            if (!_repo.Delete(id)) return NotFound();
            return NoContent();
        }

        [HttpGet("filter")]
        public ActionResult<IEnumerable<VideoCard>> FilterByManufacturer([FromQuery] string manufacturer)
            => Ok(_repo.FindByManufacturer(manufacturer));
    }

    // Tests using Moq for all repository interactions and correct model property names
    public class VideoCardsControllerTests
    {
        private Mock<IVideoCardRepository> CreateMockRepoWithSeed()
        {
            var seed = new List<VideoCard>
            {
                new VideoCard
                {
                    Id = 1,
                    ModelName = "Alpha",
                    ManufacturerId = 1,
                    Manufacturer = new Manufacturer { Id = 1, Name = "Nvidia" },
                    Price = 499.99m
                },
                new VideoCard
                {
                    Id = 2,
                    ModelName = "Beta",
                    ManufacturerId = 2,
                    Manufacturer = new Manufacturer { Id = 2, Name = "AMD" },
                    Price = 299.99m
                },
                new VideoCard
                {
                    Id = 3,
                    ModelName = "Gamma",
                    ManufacturerId = 1,
                    Manufacturer = new Manufacturer { Id = 1, Name = "Nvidia" },
                    Price = 699.99m
                }
            };

            // backing store for the mock repository (cloned to avoid shared refs)
            var items = seed.Select(Clone).ToList();

            var mock = new Mock<IVideoCardRepository>();

            mock.Setup(r => r.GetAll())
                .Returns(() => items.Select(Clone));

            mock.Setup(r => r.GetById(It.IsAny<int>()))
                .Returns((int id) =>
                {
                    var v = items.FirstOrDefault(i => i.Id == id);
                    return v == null ? null : Clone(v);
                });

            mock.Setup(r => r.Add(It.IsAny<VideoCard>()))
                .Callback((VideoCard card) =>
                {
                    // ensure new entry is cloned
                    items.Add(Clone(card));
                });

            mock.Setup(r => r.Update(It.IsAny<VideoCard>()))
                .Returns((VideoCard card) =>
                {
                    var idx = items.FindIndex(i => i.Id == card.Id);
                    if (idx == -1) return false;
                    items[idx] = Clone(card);
                    return true;
                });

            mock.Setup(r => r.Delete(It.IsAny<int>()))
                .Returns((int id) =>
                {
                    var existing = items.FirstOrDefault(i => i.Id == id);
                    if (existing == null) return false;
                    items.Remove(existing);
                    return true;
                });

            mock.Setup(r => r.FindByManufacturer(It.IsAny<string>()))
                .Returns((string manufacturer) =>
                    items
                        .Where(i => string.Equals(i.Manufacturer?.Name, manufacturer, StringComparison.OrdinalIgnoreCase))
                        .Select(Clone)
                );

            return mock;
        }

        private static VideoCard Clone(VideoCard src) => new VideoCard
        {
            Id = src.Id,
            ModelName = src.ModelName,
            Price = src.Price,
            ManufacturerId = src.ManufacturerId,
            Manufacturer = src.Manufacturer == null ? null : new Manufacturer
            {
                Id = src.Manufacturer.Id,
                Name = src.Manufacturer.Name
            },
            ImageUrl = src.ImageUrl,
            Description = src.Description
        };

        [Fact]
        public void IndexReturnsAll()
        {
            var mock = CreateMockRepoWithSeed();
            var controller = new VideoCardsController(mock.Object);

            var result = controller.Get();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<VideoCard>>(ok.Value);
            Assert.Equal(3, list.Count());
        }

        [Fact]
        public void Details_ReturnsNotFound_WhenMissing()
        {
            var mock = CreateMockRepoWithSeed();
            var controller = new VideoCardsController(mock.Object);

            var result = controller.Get(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void Details_ReturnsVideoCard()
        {
            var mock = CreateMockRepoWithSeed();
            var controller = new VideoCardsController(mock.Object);

            var result = controller.Get(2);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var card = Assert.IsType<VideoCard>(ok.Value);
            Assert.Equal(2, card.Id);
            Assert.Equal("Beta", card.ModelName);
            Assert.NotNull(card.Manufacturer);
            Assert.Equal("AMD", card.Manufacturer!.Name);
        }

        [Fact]
        public void Create_ReturnsCreatedAndAdds()
        {
            var mock = CreateMockRepoWithSeed();
            var controller = new VideoCardsController(mock.Object);

            var newCard = new VideoCard
            {
                Id = 10,
                ModelName = "Delta",
                ManufacturerId = 3,
                Manufacturer = new Manufacturer { Id = 3, Name = "Intel" },
                Price = 199.99m
            };

            var result = controller.Post(newCard);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var card = Assert.IsType<VideoCard>(created.Value);
            Assert.Equal(10, card.Id);
            Assert.Equal("Delta", card.ModelName);

            // verify repository now contains the item via mock backing list through GetById
            var fetched = mock.Object.GetById(10);
            Assert.NotNull(fetched);
            Assert.Equal("Delta", fetched!.ModelName);
            Assert.NotNull(fetched.Manufacturer);
            Assert.Equal("Intel", fetched.Manufacturer!.Name);
        }

        [Fact]
        public void Create_ReturnsBadRequestOnDuplicate()
        {
            var mock = CreateMockRepoWithSeed();
            var controller = new VideoCardsController(mock.Object);

            var duplicate = new VideoCard
            {
                Id = 1,
                ModelName = "AlphaCopy",
                ManufacturerId = 1,
                Manufacturer = new Manufacturer { Id = 1, Name = "Nvidia" },
                Price = 499.99m
            };

            var result = controller.Post(duplicate);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public void Edit_ReturnsNoContentAndUpdates()
        {
            var mock = CreateMockRepoWithSeed();
            var controller = new VideoCardsController(mock.Object);

            var updated = new VideoCard
            {
                Id = 2,
                ModelName = "Beta-Updated",
                ManufacturerId = 2,
                Manufacturer = new Manufacturer { Id = 2, Name = "AMD" },
                Price = 349.99m
            };

            var result = controller.Put(2, updated);

            Assert.IsType<NoContentResult>(result);

            var fetched = mock.Object.GetById(2);
            Assert.NotNull(fetched);
            Assert.Equal("Beta-Updated", fetched!.ModelName);
            Assert.Equal(349.99m, fetched.Price);
        }

        [Fact]
        public void Edit_ReturnsNotFound()
        {
            var mock = CreateMockRepoWithSeed();
            var controller = new VideoCardsController(mock.Object);

            var updated = new VideoCard
            {
                Id = 999,
                ModelName = "NonExistent",
                ManufacturerId = 99,
                Manufacturer = new Manufacturer { Id = 99, Name = "X" },
                Price = 0m
            };

            var result = controller.Put(999, updated);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Delete_ReturnsNoContentAndRemoves()
        {
            var mock = CreateMockRepoWithSeed();
            var controller = new VideoCardsController(mock.Object);

            var result = controller.Delete(1);
            Assert.IsType<NoContentResult>(result);

            var getResult = controller.Get(1);
            Assert.IsType<NotFoundResult>(getResult.Result);
        }

        [Fact]
        public void FilterByManufacturer_ReturnsFilteredList()
        {
            var mock = CreateMockRepoWithSeed();
            var controller = new VideoCardsController(mock.Object);

            var result = controller.FilterByManufacturer("Nvidia");
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<VideoCard>>(ok.Value);
            Assert.Equal(2, list.Count());
            Assert.All(list, c =>
            {
                Assert.NotNull(c.Manufacturer);
                Assert.Equal("Nvidia", c.Manufacturer!.Name, ignoreCase: true);
            });
        }
    }
}
