using GPUStore.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUStoreTests
{
    public class VideoCardModelValidationTests
    {
        // Помощен метод за задействане на вградената .NET валидация
        private IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }

        [Fact]
        public void VideoCard_Should_Pass_With_Valid_Data()
        {
            // Arrange
            var card = new VideoCard
            {
                ModelName = "NVIDIA RTX 5090",
                Price = 3500.00m,
                ManufacturerId = 1
            };

            // Act
            var results = ValidateModel(card);

            // Assert
            Assert.Empty(results); // Очакваме 0 грешки
        }

        [Fact]
        public void VideoCard_Should_Fail_When_ModelName_Is_Missing()
        {
            // Arrange
            var card = new VideoCard
            {
                ModelName = null, // [Required] ще гръмне тук
                Price = 1500.00m
            };

            // Act
            var results = ValidateModel(card);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains("ModelName"));
        }

        [Theory]
        [InlineData(-100.50)]
        [InlineData(0)]
        public void VideoCard_Should_Fail_When_Price_Is_Not_Positive(decimal invalidPrice)
        {
            // Arrange
            // Забележка: За да работи този тест, добави [Range(0.01, double.MaxValue)] към Price в модела
            var card = new VideoCard
            {
                ModelName = "Test Card",
                Price = invalidPrice
            };

            // Act
            var results = ValidateModel(card);

            // Assert
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains("Price"));
        }

        [Fact]
        public void VideoCard_Collections_Should_Be_Initialized_By_Default()
        {
            // Arrange & Act
            var card = new VideoCard();

            // Assert - проверяваме дали инстанцирането на списъците в модела работи
            Assert.NotNull(card.CardTechnologies);
            Assert.NotNull(card.OrderItems);
        }
    }
}
