namespace BankSystem.Services.Tests.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BankSystem.Models;
    using Card;
    using Common;
    using Data;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Models.Card;
    using Xunit;

    public class CardServiceTests : BaseTest
    {
        public CardServiceTests()
        {
            dbContext = DatabaseInstance;
            cardService = new CardService(
                dbContext,
                new CardHelper(MockedBankConfiguration.Object),
                Mapper);
        }

        private const string SampleCardId = "de88436d-5761-4512-998b-40d8264aba37";
        private const string SampleUserId = "sdgsfcx-arq12wsdxcvc";
        private const string SampleAccountId = "ABC125trABSD1";
        private const string SampleNumber = "1017840221397613";
        private const string SampleSecurityCode = "685";
        private const string SampleExpiryDate = "08/22";
        private const string SampleName = "melik";

        private readonly BankSystemDbContext dbContext;
        private readonly ICardService cardService;

        [Theory]
        [InlineData("03015135")]
        [InlineData("030124135")]
        [InlineData("01")]
        [InlineData("-10")]
        public async Task CreateAsync_WithInvalidExpiryDate_Should_ReturnFalse(string expiryDate)
        {
            // Set invalid expiryDate
            CardCreateServiceModel model = new CardCreateServiceModel
            {
                Name = SampleName,
                UserId = SampleUserId,
                AccountId = SampleAccountId,
                ExpiryDate = expiryDate
            };

            // Act
            var result = await cardService.CreateAsync(model);

            // Assert
            result
                .Should()
                .BeFalse();
        }

        [Theory]
        [InlineData("03015135")]
        [InlineData("030124135")]
        [InlineData("01")]
        [InlineData("-10")]
        public async Task CreateAsync_WithInvalidExpiryDate_Should_Not_InsertInDatabase(string expiryDate)
        {
            // Set invalid expiryDate
            CardCreateServiceModel model = new CardCreateServiceModel
            {
                Name = SampleName,
                UserId = SampleUserId,
                AccountId = SampleAccountId,
                ExpiryDate = expiryDate
            };

            // Act
            await cardService.CreateAsync(model);

            // Assert
            dbContext
                .Cards
                .Should()
                .BeEmpty();
        }

        [Theory]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("!")]
        [InlineData(" sdgsfcx-arq12wsdxcvc")]
        public async Task GetAllCardsAsync_WithInvalidUserId_Should_ReturnEmptyCollection(string userId)
        {
            // Arrange
            await SeedCardAsync();

            // Act
            IEnumerable<CardDetailsServiceModel> result = await cardService.GetCardsAsync<CardDetailsServiceModel>(userId);

            // Assert
            result
                .Should()
                .BeNullOrEmpty();
        }

        [Fact]
        public async Task GetCountOfAllCardsOwnedByUserAsync_Should_ReturnCorrectCount()
        {
            // Arrange
            await SeedCardAsync();

            // Act
            var result = await cardService.GetCountOfAllCardsOwnedByUserAsync(SampleUserId);

            // Assert
            result
                .Should()
                .Be(await dbContext.Cards.CountAsync(c => c.UserId == SampleUserId));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("209358)(%#*@)(%*#$)ET(WFI)SD")]
        [InlineData(" 1  4 10")]
        public async Task DeleteAsync_WithInvalidId_Should_ReturnFalse(string id)
        {
            // Arrange
            await SeedCardAsync();

            // Act
            var result = await cardService.DeleteAsync(id);

            // Assert
            result
                .Should()
                .BeFalse();
        }

        [Theory]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("209358)(%#*@)(%*#$)ET(WFI)SD")]
        [InlineData(" 1  4 10")]
        public async Task DeleteAsync_WithInvalidId_Should_Not_DeleteCardFromDatabase(string id)
        {
            // Arrange
            await SeedCardAsync();

            // Act
            await cardService.DeleteAsync(id);

            // Assert
            dbContext
                .Cards
                .Should()
                .HaveCount(1);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidModel_Should_ReturnFalse()
        {
            // Act
            var result = await cardService.CreateAsync(new CardCreateServiceModel());

            // Assert
            result
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task CreateAsync_WithInvalidModel_Should_Not_InsertInDatabase()
        {
            // Act
            await cardService.CreateAsync(new CardCreateServiceModel());

            // Assert
            dbContext
                .Cards
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task CreateAsync_WithInvalidName_Should_ReturnFalse()
        {
            // Set invalid name
            CardCreateServiceModel model = new CardCreateServiceModel
            {
                Name = new string('m', ModelConstants.Card.NameMaxLength + 1),
                UserId = SampleUserId,
                AccountId = SampleAccountId,
                ExpiryDate = SampleExpiryDate
            };

            // Act
            var result = await cardService.CreateAsync(model);

            // Assert
            result
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task CreateAsync_WithInvalidName_Should_Not_InsertInDatabase()
        {
            // Set invalid name
            CardCreateServiceModel model = new CardCreateServiceModel
            {
                Name = new string('m', ModelConstants.Card.NameMaxLength + 1),
                UserId = SampleUserId,
                AccountId = SampleAccountId,
                ExpiryDate = SampleExpiryDate
            };

            // Act
            await cardService.CreateAsync(model);

            // Assert
            dbContext
                .Cards
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task CreateAsync_WithValidModel_Should_ReturnTrue()
        {
            // Arrange
            await SeedUserAsync();
            CardCreateServiceModel model = new CardCreateServiceModel
            {
                Name = SampleName,
                UserId = SampleUserId,
                AccountId = SampleAccountId,
                ExpiryDate = SampleExpiryDate
            };

            // Act
            var result = await cardService.CreateAsync(model);

            // Assert
            result
                .Should()
                .BeTrue();
        }

        [Fact]
        public async Task CreateAsync_WithValidModel_Should_InsertInDatabase()
        {
            // Arrange
            var dbCount = dbContext.Accounts.Count();

            await SeedUserAsync();
            CardCreateServiceModel model = new CardCreateServiceModel
            {
                Name = SampleName,
                UserId = SampleUserId,
                AccountId = SampleAccountId,
                ExpiryDate = SampleExpiryDate
            };

            // Act
            await cardService.CreateAsync(model);

            // Assert
            dbContext
                .Cards
                .Should()
                .HaveCount(dbCount + 1);
        }


        [Fact]
        public async Task DeleteAsync_WithValidId_Should_ReturnTrue()
        {
            // Arrange
            Card model = await SeedCardAsync();

            // Act
            var result = await cardService.DeleteAsync(model.Id);

            // Assert
            result
                .Should()
                .BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_Should_DeleteCardFromDatabase()
        {
            // Arrange
            Card model = await SeedCardAsync();

            // Act
            await cardService.DeleteAsync(model.Id);

            // Assert
            dbContext
                .Cards
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task GetAllCardsAsync_WithValidUserId_Should_ReturnCorrectCount()
        {
            // Arrange
            Card model = await SeedCardAsync();

            // Act
            IEnumerable<CardDetailsServiceModel> result = await cardService.GetCardsAsync<CardDetailsServiceModel>(model.UserId);

            // Assert
            result
                .Should()
                .HaveCount(1);
        }

        [Fact]
        public async Task GetAllCardsAsync_WithValidUserId_Should_ReturnCorrectEntities()
        {
            // Arrange
            Card model = await SeedCardAsync();

            // Act
            IEnumerable<CardDetailsServiceModel> result = await cardService.GetCardsAsync<CardDetailsServiceModel>(model.UserId);

            // Assert
            result
                .Should()
                .AllBeAssignableTo<CardDetailsServiceModel>()
                .And
                .Match<IEnumerable<CardDetailsServiceModel>>(x => x.All(c => c.UserId == model.UserId));
        }

        [Fact]
        public async Task GetAsync_WithInvalidId_Should_ReturnNull()
        {
            // Arrange
            await SeedCardAsync();

            // Act
            CardDetailsServiceModel result = await cardService.GetAsync<CardDetailsServiceModel>(null);

            // Assert
            result
                .Should()
                .BeNull();
        }

        [Fact]
        public async Task GetAsync_WithInvalidParameters_Should_ReturnNull()
        {
            // Arrange
            Card model = await SeedCardAsync();

            // Act
            CardDetailsServiceModel result = await cardService.GetAsync<CardDetailsServiceModel>("wrong number", model.ExpiryDate,
                model.SecurityCode, model.User.FullName);

            // Assert
            result
                .Should()
                .BeNull();
        }

        [Fact]
        public async Task GetAsync_WithValidId_Should_ReturnCorrectEntity()
        {
            // Arrange
            Card model = await SeedCardAsync();
            var expectedId = model.Id;

            // Act
            CardDetailsServiceModel result = await cardService.GetAsync<CardDetailsServiceModel>(expectedId);

            // Assert
            result
                .Should()
                .NotBeNull()
                .And
                .Match(x => x.As<CardDetailsServiceModel>().Id == expectedId);
        }

        [Fact]
        public async Task GetAsync_WithValidParameters_Should_ReturnCorrectEntity()
        {
            // Arrange
            Card model = await SeedCardAsync();

            // Act
            CardDetailsServiceModel result = await cardService.GetAsync<CardDetailsServiceModel>(model.Number, model.ExpiryDate,
                model.SecurityCode, model.User.FullName);

            // Assert
            result
                .Should()
                .NotBeNull()
                .And
                .Match(x => x.As<CardDetailsServiceModel>().Id == model.Id);
        }


        private async Task<Card> SeedCardAsync()
        {
            await SeedUserAsync();
            Card model = new Card
            {
                Id = SampleCardId,
                Name = SampleName,
                Account = new BankAccount(),
                ExpiryDate = SampleExpiryDate,
                Number = SampleNumber,
                SecurityCode = SampleSecurityCode,
                User = await dbContext.Users.FirstOrDefaultAsync()
            };

            await dbContext.Cards.AddAsync(model);
            await dbContext.SaveChangesAsync();

            return model;
        }

        private async Task SeedUserAsync()
        {
            await dbContext.Users.AddAsync(new BankUser { Id = SampleUserId, FullName = SampleName });
            await dbContext.SaveChangesAsync();
        }
    }
}