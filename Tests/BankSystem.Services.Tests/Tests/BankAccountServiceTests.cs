using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankSystem.Data;
using BankSystem.Models;
using BankSystem.Services.BankAccount;
using BankSystem.Services.Models.BankAccount;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BankSystem.Services.Tests.Tests
{
    public class BankAccountServiceTests : BaseTest
    {
        private const string SampleBankAccountName = "Test bank account name";
        private const string SampleBankAccountUserId = "adfsdvxc-123ewsf";
        private const string SampleBankAccountId = "1";
        private const string SampleBankAccountUniqueId = "UniqueId";
        private readonly IBankAccountService bankAccountService;

        private readonly BankSystemDbContext dbContext;

        public BankAccountServiceTests()
        {
            dbContext = DatabaseInstance;
            bankAccountService = new BankAccountService(dbContext,
                new BankAccountUniqueIdHelper(MockedBankConfiguration.Object), Mapper);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("   ")]
        [InlineData("")]
        [InlineData("someRandomValue")]
        public async Task ChangeAccountNameAsync_WithInvalidId_Should_ReturnFalse(string id)
        {
            // Arrange
            await SeedBankAccountAsync();

            // Act
            var result = await bankAccountService.ChangeAccountNameAsync(id, SampleBankAccountName);

            // Assert
            result
                .Should()
                .BeFalse();
        }

        private async Task SeedUserAsync()
        {
            await dbContext.Users.AddAsync(new BankUser
                { Id = SampleBankAccountUserId, FullName = SampleBankAccountUniqueId });
            await dbContext.SaveChangesAsync();
        }

        private async Task<BankSystem.Models.BankAccount> SeedBankAccountAsync()
        {
            BankSystem.Models.BankAccount model = new BankSystem.Models.BankAccount
            {
                Id = SampleBankAccountId,
                Name = SampleBankAccountName,
                UniqueId = SampleBankAccountUniqueId,
                UserId = SampleBankAccountUserId
            };
            await dbContext.Accounts.AddAsync(model);
            await dbContext.SaveChangesAsync();

            return model;
        }

        [Fact]
        public async Task ChangeAccountNameAsync_WithValidId_Should_ReturnTrue_And_ChangeNameSuccessfully()
        {
            // Arrange
            BankSystem.Models.BankAccount model = await SeedBankAccountAsync();
            var newName = "changed!";

            // Act
            var result = await bankAccountService.ChangeAccountNameAsync(model.Id, newName);

            // Assert
            result
                .Should()
                .BeTrue();

            // Ensure that name is changed
            BankSystem.Models.BankAccount dbModel = await dbContext
                .Accounts
                .FindAsync(model.Id);

            dbModel.Name
                .Should()
                .BeEquivalentTo(newName);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidNameLength_Should_ReturnNull_And_NotInsertInDatabase()
        {
            // Arrange
            await SeedUserAsync();
            // Name is invalid when it's longer than 35 characters
            BankAccountCreateServiceModel model = new BankAccountCreateServiceModel
                { Name = new string('c', 36), UserId = SampleBankAccountUserId };

            // Act
            var result = await bankAccountService.CreateAsync(model);

            // Assert
            result
                .Should()
                .BeNull();

            dbContext
                .Accounts
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task CreateAsync_WithInvalidUserId_Should_ReturnNull_And_NotInsertInDatabase()
        {
            // Arrange
            await SeedUserAsync();
            BankAccountCreateServiceModel model = new BankAccountCreateServiceModel
                { Name = SampleBankAccountName, UserId = null };

            // Act
            var result = await bankAccountService.CreateAsync(model);

            // Assert
            result
                .Should()
                .BeNull();

            dbContext
                .Accounts
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task CreateAsync_WithValidModel_AndEmptyName_Should_SetRandomString()
        {
            // Arrange
            var count = dbContext.Accounts.Count();
            await SeedUserAsync();
            BankAccountCreateServiceModel model = new BankAccountCreateServiceModel
                { UserId = SampleBankAccountUserId };

            // Act
            var result = await bankAccountService.CreateAsync(model);

            // Assert
            result
                .Should()
                .NotBeNullOrEmpty()
                .And
                .BeAssignableTo<string>();

            dbContext
                .Accounts
                .Should()
                .HaveCount(count + 1);
        }

        [Fact]
        public async Task CreateAsync_WithValidModel_Should__InsertInDatabase()
        {
            // Arrange
            var count = dbContext.Accounts.Count();
            await SeedUserAsync();
            BankAccountCreateServiceModel model = new BankAccountCreateServiceModel
                { UserId = SampleBankAccountUserId };

            // Act
            await bankAccountService.CreateAsync(model);

            dbContext
                .Accounts
                .Should()
                .HaveCount(count + 1);
        }

        [Fact]
        public async Task CreateAsync_WithValidModel_Should_ReturnNonEmptyString()
        {
            // Arrange
            await SeedUserAsync();
            // CreatedOn is not required since it has default value which is set from the class - Datetime.UtcNow
            BankAccountCreateServiceModel model = new BankAccountCreateServiceModel
                { Name = SampleBankAccountName, UserId = SampleBankAccountUserId, CreatedOn = DateTime.UtcNow };

            // Act
            var result = await bankAccountService.CreateAsync(model);

            // Assert
            result
                .Should()
                .NotBeNullOrEmpty()
                .And
                .BeAssignableTo<string>();
        }

        [Fact]
        public async Task GetAccountsAsync_Should_ReturnCollectionWithCorrectModels()
        {
            // Arrange
            await SeedBankAccountAsync();

            // Act
            IEnumerable<BankAccountDetailsServiceModel> result =
                await bankAccountService.GetAccountsAsync<BankAccountDetailsServiceModel>();

            // Assert
            result
                .Should()
                .AllBeAssignableTo<IEnumerable<BankAccountDetailsServiceModel>>();
        }

        [Fact]
        public async Task GetAllAccountsByUserIdAsync_WithInvalidId_Should_ReturnEmptyModel()
        {
            // Arrange
            await SeedBankAccountAsync();
            // Act
            IEnumerable<BankAccountIndexServiceModel> result =
                await bankAccountService.GetAllAccountsByUserIdAsync<BankAccountIndexServiceModel>(null);

            // Assert
            result
                .Should()
                .BeNullOrEmpty();
        }

        [Fact]
        public async Task GetAllAccountsByUserIdAsync_WithValidId_Should_ReturnCorrectModel()
        {
            // Arrange
            BankSystem.Models.BankAccount model = await SeedBankAccountAsync();
            // Act
            IEnumerable<BankAccountIndexServiceModel> result =
                await bankAccountService.GetAllAccountsByUserIdAsync<BankAccountIndexServiceModel>(model.UserId);

            // Assert
            result
                .Should()
                .BeAssignableTo<IEnumerable<BankAccountIndexServiceModel>>();
        }

        [Fact]
        public async Task GetCountOfAccountsAsync_Should_ReturnCorrectCount()
        {
            // Arrange
            await SeedBankAccountAsync();

            // Act
            var result =
                await bankAccountService.GetCountOfAccountsAsync();

            // Assert
            result
                .Should()
                .Be(await dbContext.Accounts.CountAsync());
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidBankAccountId_Should_ReturnNull()
        {
            // Arrange
            await SeedBankAccountAsync();

            // Act
            BankAccountConciseServiceModel result =
                await bankAccountService.GetByIdAsync<BankAccountConciseServiceModel>(null);

            // Arrange
            result
                .Should()
                .BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithValidBankAccountId_Should_ReturnCorrectEntity()
        {
            // Arrange
            BankSystem.Models.BankAccount model = await SeedBankAccountAsync();
            var expectedId = model.Id;
            var expectedUniqueId = model.UniqueId;

            // Act
            BankAccountIndexServiceModel result =
                await bankAccountService.GetByIdAsync<BankAccountIndexServiceModel>(model.Id);

            // Arrange
            result
                .Should()
                .NotBeNull()
                .And
                .Match(x => x.As<BankAccountIndexServiceModel>().Id == expectedId)
                .And
                .Match(x => x.As<BankAccountIndexServiceModel>().UniqueId == expectedUniqueId);
        }

        [Fact]
        public async Task GetByUniqueIdAsync_WithInvalidUniqueId_Should_ReturnNull()
        {
            // Arrange
            await SeedBankAccountAsync();

            // Act
            BankAccountIndexServiceModel result =
                await bankAccountService.GetByUniqueIdAsync<BankAccountIndexServiceModel>(null);

            // Arrange
            result
                .Should()
                .BeNull();
        }

        [Fact]
        public async Task GetByUniqueIdAsync_WithValidUniqueId_Should_ReturnCorrectEntity()
        {
            // Arrange
            BankSystem.Models.BankAccount model = await SeedBankAccountAsync();
            var expectedUniqueId = model.UniqueId;

            // Act
            BankAccountIndexServiceModel result =
                await bankAccountService.GetByUniqueIdAsync<BankAccountIndexServiceModel>(model.UniqueId);

            // Arrange
            result
                .Should()
                .NotBeNull()
                .And
                .Match(x => x.As<BankAccountIndexServiceModel>().UniqueId == expectedUniqueId);
        }
    }
}