using BankSystem.Models;

namespace BankSystem.Services.User
{
    using System.Threading.Tasks;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class UserService : BaseService, IUserService
    {
        public UserService(BankSystemDbContext context)
            : base(context)
        {
        }

        public async Task<string> GetUserIdByUsernameAsync(string username)
        {
            BankUser user = await Context
                .Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.UserName == username);

            return user?.Id;
        }

        public async Task<string> GetAccountOwnerFullnameAsync(string userId)
        {
            BankUser user = await Context
                .Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == userId);

            return user?.FullName;
        }
    }
}