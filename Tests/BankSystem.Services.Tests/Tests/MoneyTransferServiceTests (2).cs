namespace BankSystem.Services.Tests.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BankSystem.Models;
    using Common;
    using Common.EmailSender.Interface;
    using Data;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Models.MoneyTransfer;
    using MoneyTransfer;
    using Moq;
    using Xunit;

    public class MoneyTransferServiceTests : BaseTest
    {
        public MoneyTransferServiceTests()
        {
            dbContext = DatabaseInstance;
            moneyTransferService = new MoneyTransferService(
                dbContext,
                Mock.Of<IEmailSender>(),
                Mapper);
        }

        private const string SampleId = "gsdxcvew-sdfdscx-xcgds";
        private const string SampleDescription = "I'm sending money due to...";
        private const decimal SampleAmount = 10;
        private const string SampleRecipientName = "test";
        private const string SampleSenderName = "melik";
        private const string SampleDestination = "dgsfcx-arq12wasdasdzxxcv";

        private const string SampleBankAccountName = "Test bank account name";
        private const string SampleBankAccountId = "1";
        private const string SampleBankAccountUniqueId = "UniqueId";
        private const string SampleReferenceNumber = "18832258557125540";

        private const string SampleUserFullname = "user user";
        private const string SampleUserId = "adfsdvxc-123ewsf";

        private readonly BankSystemDbContext dbContext;
        private readonly IMoneyTransferService moneyTransferService;

        [Theory]
        [InlineData(" 031069130864508423")]
        [InlineData("24675875i6452436 ")]
        [InlineData("! 68o4473485669 ")]
        [InlineData("   845768798069  10   ")]
        [InlineData("45848o02835yu56=")]
        public async Task GetMoneyTransferAsync_WithInvalidNumber_Should_ReturnEmptyCollection(string referenceNumber)
        {
            // Arrange
            await SeedMoneyTransfersAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService
                    .GetMoneyTransferAsync<MoneyTransferListingServiceModel>(referenceNumber);

            // Assert
            result
                .Should()
                .BeNullOrEmpty();
        }

        [Fact]
        public async Task GetMoneyTransferAsync_WithValidNumber_Should_ReturnCorrectEntities()
        {
            // Arrange
            const string expectedRefNumber = SampleReferenceNumber;
            await dbContext.Transfers.AddAsync(new MoneyTransfer
            {
                Description = SampleDescription,
                Amount = SampleAmount,
                Account = await dbContext.Accounts.FirstOrDefaultAsync(),
                Destination = SampleDestination,
                Source = SampleBankAccountId,
                SenderName = SampleSenderName,
                RecipientName = SampleRecipientName,
                MadeOn = DateTime.UtcNow,
                ReferenceNumber = expectedRefNumber,
            });
            await dbContext.SaveChangesAsync();

            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService.GetMoneyTransferAsync<MoneyTransferListingServiceModel>(
                    expectedRefNumber);

            // Assert
            result
                .Should()
                .NotBeNullOrEmpty()
                .And
                .Match(t => t.All(x => x.ReferenceNumber == expectedRefNumber));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(" dassdgdf")]
        [InlineData(" ")]
        [InlineData("!  ")]
        [InlineData("     10   ")]
        [InlineData("  sdgsfcx-arq12wasdasdzxxcvc   ")]
        public async Task GetAllMoneyTransfersAsync_WithInvalidUserId_Should_ReturnEmptyCollection(string userId)
        {
            // Arrange
            await SeedMoneyTransfersAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService.GetMoneyTransfersAsync<MoneyTransferListingServiceModel>(userId);

            // Assert
            result
                .Should()
                .BeNullOrEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(" dassdgdf")]
        [InlineData(" ")]
        [InlineData("!  ")]
        [InlineData("     10   ")]
        [InlineData("  sdgsfcx-arq12wasdasdzxxcvc   ")]
        public async Task GetAllMoneyTransfersForAccountAsync_WithInvalidUserId_Should_ReturnEmptyCollection(
            string accountId)
        {
            // Arrange
            await SeedMoneyTransfersAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService
                    .GetMoneyTransfersForAccountAsync<MoneyTransferListingServiceModel>(accountId);

            // Assert
            result
                .Should()
                .BeNullOrEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(" dassdgdf")]
        [InlineData(" ")]
        [InlineData("!  ")]
        [InlineData("     10   ")]
        [InlineData("  sdgsfcx-arq12wasdasdzxxcvc   ")]
        public async Task GetLast10MoneyTransfersForUserAsync_WithInvalidUserId_Should_ReturnEmptyCollection(
            string userId)
        {
            // Arrange
            await SeedMoneyTransfersAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService
                    .GetLast10MoneyTransfersForUserAsync<MoneyTransferListingServiceModel>(userId);

            // Assert
            result
                .Should()
                .BeNullOrEmpty();
        }

        [Fact]
        public async Task GetCountOfAllMoneyTransfersForUserAsync_Should_ReturnCorrectCount()
        {
            // Arrange
            await SeedMoneyTransfersAsync();

            // Act
            var result =
                await moneyTransferService.GetCountOfAllMoneyTransfersForUserAsync(SampleUserId);

            // Assert
            result
                .Should()
                .Be(await dbContext.Transfers.CountAsync(t => t.Account.UserId == SampleUserId));
        }

        [Fact]
        public async Task GetCountOfAllMoneyTransfersForAccountAsync_Should_ReturnCorrectCount()
        {
            // Arrange
            await SeedMoneyTransfersAsync();

            // Act
            var result =
                await moneyTransferService.GetCountOfAllMoneyTransfersForAccountAsync(SampleBankAccountId);

            // Assert
            result
                .Should()
                .Be(await dbContext.Transfers.CountAsync(t => t.AccountId == SampleBankAccountId));
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithDifferentId_Should_ReturnFalse()
        {
            // Arrange
            await SeedBankAccountAsync();
            MoneyTransferCreateServiceModel model = PrepareCreateModel(accountId: "another id");

            // Act
            var result = await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            result
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithDifferentId_Should_Not_AddMoneyTransferInDatabase()
        {
            // Arrange
            await SeedBankAccountAsync();
            MoneyTransferCreateServiceModel model = PrepareCreateModel(accountId: "another id");

            // Act
            await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            dbContext
                .Transfers
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithEmptyModel_Should_ReturnFalse()
        {
            // Act
            var result =
                await moneyTransferService.CreateMoneyTransferAsync(new MoneyTransferCreateServiceModel());

            // Assert
            result
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithEmptyModel_Should_Not_InsertInDatabase()
        {
            // Act
            await moneyTransferService.CreateMoneyTransferAsync(new MoneyTransferCreateServiceModel());

            // Assert
            dbContext
                .Transfers
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithInvalidDescription_Should_ReturnFalse()
        {
            // Arrange
            var invalidDescription = new string('m', ModelConstants.MoneyTransfer.DescriptionMaxLength + 1);
            MoneyTransferCreateServiceModel model = PrepareCreateModel(invalidDescription);

            // Act
            var result = await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            result
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithInvalidDescription_Should_Not_InsertInDatabase()
        {
            // Arrange
            var invalidDescription = new string('m', ModelConstants.MoneyTransfer.DescriptionMaxLength + 1);
            MoneyTransferCreateServiceModel model = PrepareCreateModel(invalidDescription);

            // Act
            await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            dbContext
                .Transfers
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithInvalidDestinationBankAccountUniqueId_Should_ReturnFalse()
        {
            // Arrange
            var invalidDestinationBankAccountUniqueId =
                new string('m', ModelConstants.BankAccount.UniqueIdMaxLength + 1);
            MoneyTransferCreateServiceModel model = PrepareCreateModel(destinationBankUniqueId: invalidDestinationBankAccountUniqueId);

            // Act
            var result = await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            result
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task
            CreateMoneyTransferAsync_WithInvalidDestinationBankAccountUniqueId_Should_Not_InsertInDatabase()
        {
            // Arrange
            var invalidDestinationBankAccountUniqueId =
                new string('m', ModelConstants.BankAccount.UniqueIdMaxLength + 1);
            MoneyTransferCreateServiceModel model = PrepareCreateModel(destinationBankUniqueId: invalidDestinationBankAccountUniqueId);

            // Act
            await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            dbContext
                .Transfers
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithInvalidRecipientName_Should_ReturnFalse()
        {
            // Arrange
            var invalidRecipientName = new string('m', ModelConstants.User.FullNameMaxLength + 1);
            MoneyTransferCreateServiceModel model = PrepareCreateModel(recipientName: invalidRecipientName);

            // Act
            var result = await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            result
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithInvalidRecipientName_Should_NotInsertInDatabase()
        {
            // Arrange
            var invalidRecipientName = new string('m', ModelConstants.User.FullNameMaxLength + 1);
            MoneyTransferCreateServiceModel model = PrepareCreateModel(recipientName: invalidRecipientName);

            // Act
            await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            dbContext
                .Transfers
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithInvalidSenderName_Should_ReturnFalse()
        {
            // Arrange
            var invalidSenderName = new string('m', ModelConstants.User.FullNameMaxLength + 1);
            MoneyTransferCreateServiceModel model = PrepareCreateModel(senderName: invalidSenderName);

            // Act
            var result = await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            result
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithInvalidSenderName_Should_Not_InsertInDatabase()
        {
            // Arrange
            var invalidSenderName = new string('m', ModelConstants.User.FullNameMaxLength + 1);
            MoneyTransferCreateServiceModel model = PrepareCreateModel(senderName: invalidSenderName);

            // Act
            await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            dbContext
                .Transfers
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithInvalidSource_Should_ReturnFalse()
        {
            // Arrange
            var invalidSource = new string('m', ModelConstants.BankAccount.UniqueIdMaxLength + 1);
            MoneyTransferCreateServiceModel model = PrepareCreateModel(source: invalidSource);

            // Act
            var result = await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            result
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task
            CreateMoneyTransferAsync_WithInvalidSource_Should_Not_InsertInDatabase()
        {
            // Arrange
            var invalidSource = new string('m', ModelConstants.BankAccount.UniqueIdMaxLength + 1);
            MoneyTransferCreateServiceModel model = PrepareCreateModel(source: invalidSource);

            // Act
            await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            dbContext
                .Transfers
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithValidModel_AndNegativeAmount_Should_ReturnTrue()
        {
            // Arrange
            var dbCount = dbContext.Transfers.Count();
            await SeedBankAccountAsync();
            // Setting amount to negative means we're sending money.
            MoneyTransferCreateServiceModel model = PrepareCreateModel(amount: -10);

            // Act
            var result = await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            result
                .Should()
                .BeTrue();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithValidModel_AndNegativeAmount_Should_InsertInDatabase()
        {
            // Arrange
            var dbCount = dbContext.Transfers.Count();
            await SeedBankAccountAsync();
            // Setting amount to negative means we're sending money.
            MoneyTransferCreateServiceModel model = PrepareCreateModel(amount: -10);

            // Act
            await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            dbContext
                .Transfers
                .Should()
                .HaveCount(dbCount + 1);
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithValidModel_Should_ReturnTrue()
        {
            // Arrange
            var dbCount = dbContext.Transfers.Count();
            await SeedBankAccountAsync();
            MoneyTransferCreateServiceModel model = PrepareCreateModel();

            // Act
            var result = await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            result
                .Should()
                .BeTrue();
        }

        [Fact]
        public async Task CreateMoneyTransferAsync_WithValidModel_Should_InsertInDatabase()
        {
            // Arrange
            var dbCount = dbContext.Transfers.Count();
            await SeedBankAccountAsync();
            MoneyTransferCreateServiceModel model = PrepareCreateModel();

            // Act
            await moneyTransferService.CreateMoneyTransferAsync(model);

            // Assert
            dbContext
                .Transfers
                .Should()
                .HaveCount(dbCount + 1);
        }

        [Fact]
        public async Task GetAllMoneyTransfersAsync_Should_ReturnCorrectCount()
        {
            // Arrange
            const int count = 10;
            await SeedBankAccountAsync();
            for (int i = 1; i <= count; i++)
            {
                await dbContext.Transfers.AddAsync(new MoneyTransfer
                {
                    Account = await dbContext.Accounts.FirstOrDefaultAsync()
                });
            }

            await dbContext.SaveChangesAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService.GetMoneyTransfersAsync<MoneyTransferListingServiceModel>(SampleUserId);

            // Assert
            result
                .Should()
                .HaveCount(count);
        }

        [Fact]
        public async Task GetAllMoneyTransfersAsync_Should_ReturnOrderedByMadeOnCollection()
        {
            // Arrange
            await SeedBankAccountAsync();
            for (int i = 0; i < 10; i++)
            {
                await dbContext.Transfers.AddAsync(new MoneyTransfer
                {
                    Id = $"{SampleId}_{i}",
                    Account = await dbContext.Accounts.FirstOrDefaultAsync(),
                    MadeOn = DateTime.UtcNow.AddMinutes(i)
                });
            }

            await dbContext.SaveChangesAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService.GetMoneyTransfersAsync<MoneyTransferListingServiceModel>(SampleUserId);

            // Assert
            result
                .Should()
                .BeInDescendingOrder(x => x.MadeOn);
        }

        [Fact]
        public async Task GetAllMoneyTransfersAsync_WithValidUserId_Should_ReturnCollectionOfCorrectEntities()
        {
            // Arrange
            MoneyTransfer model = await SeedMoneyTransfersAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService.GetMoneyTransfersAsync<MoneyTransferListingServiceModel>(model.Account
                    .UserId);

            // Assert
            result
                .Should()
                .AllBeAssignableTo<MoneyTransferListingServiceModel>()
                .And
                .Match<IEnumerable<MoneyTransferListingServiceModel>>(x => x.All(c => c.Source == model.Source));
        }

        [Fact]
        public async Task GetAllMoneyTransfersForAccountAsync_Should_ReturnCorrectCount()
        {
            // Arrange
            const int count = 10;
            await SeedBankAccountAsync();
            for (int i = 1; i <= count; i++)
            {
                await dbContext.Transfers.AddAsync(new MoneyTransfer
                {
                    Account = await dbContext.Accounts.FirstOrDefaultAsync()
                });
            }

            await dbContext.SaveChangesAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService.GetMoneyTransfersForAccountAsync<MoneyTransferListingServiceModel>(
                    SampleBankAccountId);

            // Assert
            result
                .Should()
                .HaveCount(count);
        }

        [Fact]
        public async Task GetAllMoneyTransfersForAccountAsync_Should_ReturnOrderedByMadeOnCollection()
        {
            // Arrange
            await SeedBankAccountAsync();
            for (int i = 0; i < 10; i++)
            {
                await dbContext.Transfers.AddAsync(new MoneyTransfer
                {
                    Id = $"{SampleId}_{i}",
                    Account = await dbContext.Accounts.FirstOrDefaultAsync(),
                    MadeOn = DateTime.UtcNow.AddMinutes(i)
                });
            }

            await dbContext.SaveChangesAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService.GetMoneyTransfersForAccountAsync<MoneyTransferListingServiceModel>(
                    SampleBankAccountId);

            // Assert
            result
                .Should()
                .BeInDescendingOrder(x => x.MadeOn);
        }

        [Fact]
        public async Task GetAllMoneyTransfersForAccountAsync_WithValidUserId_Should_ReturnCollectionOfCorrectEntities()
        {
            // Arrange
            MoneyTransfer model = await SeedMoneyTransfersAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService.GetMoneyTransfersForAccountAsync<MoneyTransferListingServiceModel>(
                    model.Account.Id);

            // Assert
            result
                .Should()
                .AllBeAssignableTo<MoneyTransferListingServiceModel>()
                .And
                .Match<IEnumerable<MoneyTransferListingServiceModel>>(x => x.All(c => c.Source == model.Source));
        }

        [Fact]
        public async Task GetLast10MoneyTransfersForUserAsync_Should_ReturnCorrectCount()
        {
            // Arrange
            const int count = 22;
            const int expectedCount = 10;
            await SeedBankAccountAsync();
            for (int i = 1; i <= count; i++)
            {
                await dbContext.Transfers.AddAsync(new MoneyTransfer
                {
                    Account = await dbContext.Accounts.FirstOrDefaultAsync()
                });
            }

            await dbContext.SaveChangesAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService.GetLast10MoneyTransfersForUserAsync<MoneyTransferListingServiceModel>(
                    SampleUserId);

            // Assert
            result
                .Should()
                .HaveCount(expectedCount);
        }

        [Fact]
        public async Task GetLast10MoneyTransfersForUserAsync_Should_ReturnOrderedByMadeOnCollection()
        {
            // Arrange
            await SeedBankAccountAsync();
            for (int i = 0; i < 22; i++)
            {
                await dbContext.Transfers.AddAsync(new MoneyTransfer
                {
                    Id = $"{SampleId}_{i}",
                    Account = await dbContext.Accounts.FirstOrDefaultAsync(),
                    MadeOn = DateTime.UtcNow.AddMinutes(i)
                });
            }

            await dbContext.SaveChangesAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService.GetLast10MoneyTransfersForUserAsync<MoneyTransferListingServiceModel>(
                    SampleUserId);

            // Assert
            result
                .Should()
                .BeInDescendingOrder(x => x.MadeOn);
        }

        [Fact]
        public async Task GetLast10MoneyTransfersForUserAsync_WithValidUserId_Should_ReturnCollectionOfCorrectEntities()
        {
            // Arrange
            MoneyTransfer model = await SeedMoneyTransfersAsync();
            // Act
            IEnumerable<MoneyTransferListingServiceModel> result =
                await moneyTransferService.GetLast10MoneyTransfersForUserAsync<MoneyTransferListingServiceModel>(
                    model.Account.UserId);

            // Assert
            result
                .Should()
                .AllBeAssignableTo<MoneyTransferListingServiceModel>()
                .And
                .Match<IEnumerable<MoneyTransferListingServiceModel>>(x => x.All(c => c.Source == model.Source));
        }

        private static MoneyTransferCreateServiceModel PrepareCreateModel(
            string description = SampleDescription,
            decimal amount = SampleAmount,
            string accountId = SampleBankAccountId,
            string destinationBankUniqueId = SampleDestination,
            string source = SampleBankAccountId,
            string senderName = SampleSenderName,
            string recipientName = SampleRecipientName,
            string referenceNumber = SampleReferenceNumber)
        {
            MoneyTransferCreateServiceModel model = new MoneyTransferCreateServiceModel
            {
                Description = description,
                Amount = amount,
                AccountId = accountId,
                DestinationBankAccountUniqueId = destinationBankUniqueId,
                Source = source,
                SenderName = senderName,
                RecipientName = recipientName,
                ReferenceNumber = SampleReferenceNumber,
            };

            return model;
        }

        private async Task<MoneyTransfer> SeedMoneyTransfersAsync()
        {
            await SeedBankAccountAsync();
            MoneyTransfer model = new MoneyTransfer
            {
                Id = SampleId,
                Description = SampleDescription,
                Amount = SampleAmount,
                Account = await dbContext.Accounts.FirstOrDefaultAsync(),
                Destination = SampleDestination,
                Source = SampleBankAccountId,
                SenderName = SampleSenderName,
                RecipientName = SampleRecipientName,
                MadeOn = DateTime.UtcNow
            };

            await dbContext.Transfers.AddAsync(model);
            await dbContext.SaveChangesAsync();

            return model;
        }

        private async Task<BankAccount> SeedBankAccountAsync()
        {
            await SeedUserAsync();
            BankAccount model = new BankAccount
            {
                Id = SampleBankAccountId,
                Name = SampleBankAccountName,
                UniqueId = SampleBankAccountUniqueId,
                User = await dbContext.Users.FirstOrDefaultAsync()
            };
            await dbContext.Accounts.AddAsync(model);
            await dbContext.SaveChangesAsync();

            return model;
        }

        private async Task SeedUserAsync()
        {
            await dbContext.Users.AddAsync(new BankUser { Id = SampleUserId, FullName = SampleUserFullname });
            await dbContext.SaveChangesAsync();
        }
    }
}