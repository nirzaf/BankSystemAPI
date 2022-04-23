namespace BankSystem.Web.Areas.MoneyTransfers.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper;
    using Microsoft.AspNetCore.Mvc;
    using Services.BankAccount;
    using Services.Models.BankAccount;
    using Web.Controllers;
    using Web.Models.BankAccount;

    [Area("MoneyTransfers")]
    public abstract class BaseMoneyTransferController : BaseController
    {
        private readonly IBankAccountService bankAccountService;

        protected BaseMoneyTransferController(IBankAccountService bankAccountService, IMapper mapper)
        {
            this.bankAccountService = bankAccountService;
            Mapper = mapper;
        }

        protected IMapper Mapper { get; }

        protected async Task<OwnBankAccountListingViewModel[]> GetAllAccountsAsync(string userId)
            => (await bankAccountService
                    .GetAllAccountsByUserIdAsync<BankAccountIndexServiceModel>(userId))
                .Select(Mapper.Map<OwnBankAccountListingViewModel>)
                .ToArray();
    }
}