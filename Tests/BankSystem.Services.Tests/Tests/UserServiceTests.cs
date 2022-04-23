namespace BankSystem.Services.Tests.Tests
{
    using System.Threading.Tasks;
    using BankSystem.Models;
    using Data;
    using FluentAssertions;
    using User;
    using Xunit;

    public class UserServiceTests : BaseTest
    {
        public UserServiceTests()
        {
            dbContext = DatabaseInstance;
            userService = new UserService(dbContext);
        }

        private const string SampleUserId = "dsgsdg-dsg364tr-egdfb-jfd";
        private const string SampleUsername = "melik";
        private const string SampleUserFullName = "Melik Pehlivanov";

        private readonly BankSystemDbContext dbContext;
        private readonly IUserService userService;

        [Theory]
        [InlineData(" ")]
        [InlineData("asd  1 ")]
        [InlineData("     10   ")]
        [InlineData("5215@%*)%@")]
        public async Task GetUserIdByUsernameAsync_WithInvalidUsername_Should_ReturnNull(string username)
        {
            // Arrange
            await SeedUserAsync();
            // Act
            var result = await userService.GetUserIdByUsernameAsync(username);

            // Assert
            result
                .Should()
                .BeNull();
        }

        [Theory]
        [InlineData("  !")]
        [InlineData("asd  1 ")]
        [InlineData("1246  10   ")]
        [InlineData("sdg-sdgfgscx-124r-dhf-")]
        public async Task GetAccountOwnerFullnameAsync_WithInvalidUsername_Should_ReturnNull(string id)
        {
            // Arrange
            await SeedUserAsync();
            // Act
            var result = await userService.GetAccountOwnerFullnameAsync(id);

            // Assert
            result
                .Should()
                .BeNull();
        }

        private async Task SeedUserAsync()
        {
            await dbContext.Users.AddAsync(new BankUser { Id = SampleUserId, UserName = SampleUsername, FullName = SampleUserFullName });
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAccountOwnerFullnameAsync_WithValidUsername_Should_ReturnCorrectName()
        {
            // Arrange
            await SeedUserAsync();
            // Act
            var result = await userService.GetAccountOwnerFullnameAsync(SampleUserId);

            // Assert
            result
                .Should()
                .NotBeNull()
                .And
                .Be(SampleUserFullName);
        }

        [Fact]
        public async Task GetUserIdByUsernameAsync_WithValidUsername_Should_ReturnCorrectId()
        {
            // Arrange
            await SeedUserAsync();
            // Act
            var result = await userService.GetUserIdByUsernameAsync(SampleUsername);

            // Assert
            result
                .Should()
                .NotBeNull()
                .And
                .Be(SampleUserId);
        }
    }
}
