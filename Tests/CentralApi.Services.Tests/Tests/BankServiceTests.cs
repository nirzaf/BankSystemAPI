using System.Collections.Generic;
using System.Threading.Tasks;
using CentralApi.Data;
using CentralApi.Services.Bank;
using CentralApi.Services.Models.Banks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CentralApi.Services.Tests.Tests
{
    public class BankServiceTests : BaseTest
    {
        private const string SampleBankName = "Bank system";
        private const string SampleBankCountry = "Bulgaria";
        private const string SampleBankSwiftCode = "ABC";
        private const string SamplePaymentUrl = "https://localhost:56013/pay";
        private const string SampleIdentificationNumbers = "10";
        private readonly IBanksService banksService;

        private readonly CentralApiDbContext dbContext;

        public BankServiceTests()
        {
            dbContext = DatabaseInstance;
            banksService = new BanksService(dbContext, Mapper);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("    !")]
        [InlineData("totally invalid id")]
        public async Task GetBankByIdAsync_WithInvalidId_ShouldReturnNull(string id)
        {
            // Arrange
            await SeedBanks(10);

            // Act
            BankServiceModel result = await banksService.GetBankByIdAsync<BankServiceModel>(id);

            // Assert
            result
                .Should()
                .BeNull();
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("    !")]
        [InlineData("totally invalid id")]
        public async Task GetBankByBankIdentificationCardNumbersAsync_WithInvalidIdentificationNumber_ShouldReturnNull(
            string identificationNumbers)
        {
            // Arrange
            await SeedBanks(10);

            // Act
            BankServiceModel result =
                await banksService.GetBankByBankIdentificationCardNumbersAsync<BankServiceModel>(
                    identificationNumbers);

            // Assert
            result
                .Should()
                .BeNull();
        }

        private async Task SeedBanks(int count)
        {
            List<CentralApi.Models.Bank> banks = new List<CentralApi.Models.Bank>();
            for (int i = 1; i <= count; i++)
            {
                CentralApi.Models.Bank bank = new CentralApi.Models.Bank
                {
                    Id = i.ToString(),
                    Name = $"{SampleBankName}_{i}",
                    Location = $"{SampleBankCountry}_{i}",
                    SwiftCode = $"{SampleBankSwiftCode}_{i}",
                    PaymentUrl = SamplePaymentUrl,
                    BankIdentificationCardNumbers = $"{SampleIdentificationNumbers}{i}"
                };

                banks.Add(bank);
            }

            await dbContext.Banks.AddRangeAsync(banks);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAllBanksSupportingPaymentsAsync_ShouldOrderByLocationAndThenByName()
        {
            // Arrange
            await SeedBanks(10);

            // Act
            IEnumerable<BankListingServiceModel> result =
                await banksService.GetAllBanksSupportingPaymentsAsync<BankListingServiceModel>();

            // Assert
            result
                .Should()
                .BeInAscendingOrder(b => b.Location)
                .And
                .BeInAscendingOrder(b => b.Name);
        }

        [Fact]
        public async Task GetAllBanksSupportingPaymentsAsync_ShouldReturnCorrectModel()
        {
            // Arrange
            await SeedBanks(3);

            // Act
            IEnumerable<BankListingServiceModel> result =
                await banksService.GetAllBanksSupportingPaymentsAsync<BankListingServiceModel>();

            // Assert
            result
                .Should()
                .AllBeAssignableTo<BankListingServiceModel>();
        }

        [Fact]
        public async Task GetAllBanksSupportingPaymentsAsync_ShouldReturnOnlyBanks_With_NonNullablePaymentUrls()
        {
            // Arrange
            const int count = 10;
            await SeedBanks(count);

            // Seed one more bank which doesn't support payments
            await dbContext.Banks.AddAsync(new CentralApi.Models.Bank());
            await dbContext.SaveChangesAsync();

            // Act
            IEnumerable<BankListingServiceModel> result =
                await banksService.GetAllBanksSupportingPaymentsAsync<BankListingServiceModel>();

            // Assert
            result
                .Should()
                .HaveCount(count);
        }

        [Fact]
        public async Task GetBankAsync_WithInvalidBankCountry_ShouldReturnNull()
        {
            // Arrange
            await SeedBanks(5);

            // Act
            BankServiceModel result = await banksService
                .GetBankAsync<BankServiceModel>(SampleBankName, null, SampleBankSwiftCode);

            // Assert
            result
                .Should()
                .BeNull();
        }

        [Fact]
        public async Task GetBankAsync_WithInvalidBankName_ShouldReturnNull()
        {
            // Arrange
            await SeedBanks(10);

            // Act
            BankServiceModel result = await banksService
                .GetBankAsync<BankServiceModel>(null, SampleBankCountry, SampleBankSwiftCode);

            // Assert
            result
                .Should()
                .BeNull();
        }

        [Fact]
        public async Task GetBankAsync_WithInvalidBankSwiftCode_ShouldReturnNull()
        {
            // Arrange
            await SeedBanks(3);

            // Act
            BankServiceModel result = await banksService
                .GetBankAsync<BankServiceModel>(SampleBankName, SampleBankCountry, null);

            // Assert
            result
                .Should()
                .BeNull();
        }

        [Fact]
        public async Task GetBankAsync_WithValidArguments_ShouldReturnCorrectEntity()
        {
            // Arrange
            await SeedBanks(3);
            CentralApi.Models.Bank expectedBank = await dbContext.Banks.FirstOrDefaultAsync();

            // Act
            BankListingServiceModel result = await banksService
                .GetBankAsync<BankListingServiceModel>(expectedBank.Name, expectedBank.SwiftCode,
                    expectedBank.Location);

            // Assert
            result
                .Should()
                .Match(b => b.As<BankListingServiceModel>().Id == expectedBank.Id);
        }

        [Fact]
        public async Task
            GetBankByBankIdentificationCardNumbersAsync_WitValidIdentificationNumber_ShouldReturnCorrectEntity()
        {
            // Arrange
            const string expectedId = "1";
            await dbContext.Banks.AddAsync(new CentralApi.Models.Bank
                { Id = expectedId, BankIdentificationCardNumbers = SampleIdentificationNumbers });
            await dbContext.SaveChangesAsync();

            // Act
            BankListingServiceModel result =
                await banksService.GetBankByBankIdentificationCardNumbersAsync<BankListingServiceModel>(
                    SampleIdentificationNumbers);

            // Assert
            result
                .Should()
                .Match(x => x.As<BankListingServiceModel>().Id == expectedId);
        }

        [Fact]
        public async Task GetBankByIdAsync_WitValidId_ShouldReturnCorrectEntity()
        {
            // Arrange
            await SeedBanks(10);
            CentralApi.Models.Bank bank = await dbContext.Banks.FirstOrDefaultAsync();
            // Act
            BankListingServiceModel result = await banksService.GetBankByIdAsync<BankListingServiceModel>(bank.Id);

            // Assert
            result
                .Should()
                .Match(x => x.As<BankListingServiceModel>().Id == bank.Id);
        }

        [Fact]
        public async Task GetBankByIdAsync_WitValidId_ShouldReturnCorrectModel()
        {
            // Arrange
            await SeedBanks(10);
            CentralApi.Models.Bank bank = await dbContext.Banks.FirstOrDefaultAsync();
            // Act
            BankListingServiceModel result = await banksService.GetBankByIdAsync<BankListingServiceModel>(bank.Id);

            // Assert
            result
                .Should()
                .BeAssignableTo<BankListingServiceModel>();
        }
    }
}