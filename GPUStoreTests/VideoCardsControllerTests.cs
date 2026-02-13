using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace GPUStoreTests
{
    // Domain model used by controller and repository
    public class VideoCard
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public int MemoryMB { get; set; }
        public decimal Price { get; set; }
    }

    // Repository interface
    public interface IVideoCardRepository
    {
        IEnumerable<VideoCard> GetAll();
        VideoCard? GetById(int id);
        void Add(VideoCard card);
        bool Update(VideoCard card);
        bool Delete(int id);
        IEnumerable<VideoCard> FindByManufacturer(string manufacturer);
    }

    // Simple in-memory repository implementation for tests
    public class InMemoryVideoCardRepository : IVideoCardRepository
    {
        private readonly List<VideoCard> _items;

        public InMemoryVideoCardRepository(IEnumerable<VideoCard>? seed = null)
        {
            _items = seed?.Select(c => new VideoCard
            {
                Id = c.Id,
                Name = c.Name,
                Manufacturer = c.Manufacturer,
                MemoryMB = c.MemoryMB,
                Price = c.Price
            }).ToList() ?? new List<VideoCard>();
        }

        public IEnumerable<VideoCard> GetAll() => _items.Select(i => Clone(i));

        public VideoCard? GetById(int id) => _items.FirstOrDefault(i => i.Id == id) is VideoCard v ? Clone(v) : null;

        public void Add(VideoCard card)
        {
            _items.Add(Clone(card));
        }

        public bool Update(VideoCard card)
        {
            var idx = _items.FindIndex(i => i.Id == card.Id);
            if (idx == -1) return false;
            _items[idx] = Clone(card);
            return true;
        }

        public bool Delete(int id)
        {
            var existing = _items.FirstOrDefault(i => i.Id == id);
            if (existing == null) return false;
            _items.Remove(existing);
            return true;
        }

        public IEnumerable<VideoCard> FindByManufacturer(string manufacturer)
            => _items.Where(i => string.Equals(i.Manufacturer, manufacturer, StringComparison.OrdinalIgnoreCase)).Select(Clone);

        private static VideoCard Clone(VideoCard src) => new VideoCard
        {
            Id = src.Id,
            Name = src.Name,
            Manufacturer = src.Manufacturer,
            MemoryMB = src.MemoryMB,
            Price = src.Price
        };
    }

    // Minimal API-style controller to be tested
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

    // Test class containing 9 tests
    public class VideoCardsControllerTests
    {
        private InMemoryVideoCardRepository CreateRepoWithSeed()
        {
            var seed = new[]
            {
                new VideoCard { Id = 1, Name = "Alpha", Manufacturer = "Nvidia", MemoryMB = 8192, Price = 499.99m },
                new VideoCard { Id = 2, Name = "Beta", Manufacturer = "AMD", MemoryMB = 6144, Price = 299.99m },
                new VideoCard { Id = 3, Name = "Gamma", Manufacturer = "Nvidia", MemoryMB = 12288, Price = 699.99m }
            };
            return new InMemoryVideoCardRepository(seed);
        }

        [Fact]
        public void IndexReturnsAll()
        {
            var repo = CreateRepoWithSeed();
            var controller = new VideoCardsController(repo);

            var result = controller.Get();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<VideoCard>>(ok.Value);
            Assert.Equal(3, list.Count());
        }

        [Fact]
        public void Details_ReturnsNotFound_WhenMissing()
        {
            var repo = CreateRepoWithSeed();
            var controller = new VideoCardsController(repo);

            var result = controller.Get(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void Details_ReturnsVideoCard()
        {
            var repo = CreateRepoWithSeed();
            var controller = new VideoCardsController(repo);

            var result = controller.Get(2);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var card = Assert.IsType<VideoCard>(ok.Value);
            Assert.Equal(2, card.Id);
            Assert.Equal("Beta", card.Name);
        }

        [Fact]
        public void Create_ReturnsCreatedAndAdds()
        {
            var repo = CreateRepoWithSeed();
            var controller = new VideoCardsController(repo);

            var newCard = new VideoCard { Id = 10, Name = "Delta", Manufacturer = "Intel", MemoryMB = 4096, Price = 199.99m };
            var result = controller.Post(newCard);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var card = Assert.IsType<VideoCard>(created.Value);
            Assert.Equal(10, card.Id);

            // verify repository now contains the item
            var fetched = repo.GetById(10);
            Assert.NotNull(fetched);
            Assert.Equal("Delta", fetched!.Name);
        }

        [Fact]
        public void Create_ReturnsBadRequestOnDuplicate()
        {
            var repo = CreateRepoWithSeed();
            var controller = new VideoCardsController(repo);

            var duplicate = new VideoCard { Id = 1, Name = "AlphaCopy", Manufacturer = "Nvidia", MemoryMB = 8192, Price = 499.99m };
            var result = controller.Post(duplicate);

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public void Edit_ReturnsNoContentAndUpdates()
        {
            var repo = CreateRepoWithSeed();
            var controller = new VideoCardsController(repo);

            var updated = new VideoCard { Id = 2, Name = "Beta-Updated", Manufacturer = "AMD", MemoryMB = 8192, Price = 349.99m };
            var result = controller.Put(2, updated);

            Assert.IsType<NoContentResult>(result);

            var fetched = repo.GetById(2);
            Assert.NotNull(fetched);
            Assert.Equal("Beta-Updated", fetched!.Name);
            Assert.Equal(8192, fetched.MemoryMB);
        }

        [Fact]
        public void Edit_ReturnsNotFound()
        {
            var repo = CreateRepoWithSeed();
            var controller = new VideoCardsController(repo);

            var updated = new VideoCard { Id = 999, Name = "NonExistent", Manufacturer = "X", MemoryMB = 0, Price = 0m };
            var result = controller.Put(999, updated);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Delete_ReturnsNoContentAndRemoves()
        {
            var repo = CreateRepoWithSeed();
            var controller = new VideoCardsController(repo);

            var result = controller.Delete(1);
            Assert.IsType<NoContentResult>(result);

            var getResult = controller.Get(1);
            Assert.IsType<NotFoundResult>(getResult.Result);
        }

        [Fact]
        public void FilterByManufacturer_ReturnsFilteredList()
        {
            var repo = CreateRepoWithSeed();
            var controller = new VideoCardsController(repo);

            var result = controller.FilterByManufacturer("Nvidia");
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<VideoCard>>(ok.Value);
            Assert.Equal(2, list.Count());
            Assert.All(list, c => Assert.Equal("Nvidia", c.Manufacturer, ignoreCase: true));
        }
    }
}
