namespace BankSystem.Web.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Areas.MoneyTransfers.Models;
    using AutoMapper;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Models.BankAccount;
    using Services.BankAccount;
    using Services.Models.BankAccount;
    using Services.Models.MoneyTransfer;
    using Services.MoneyTransfer;

    [AllowAnonymous]
    public class HomeController : BaseController
    {
        private readonly IBankAccountService bankAccountService;
        private readonly IMoneyTransferService moneyTransferService;
        private readonly IMapper mapper;

        public HomeController(
            IBankAccountService bankAccountService,
            IMoneyTransferService moneyTransferService,
            IMapper mapper)
        {
            this.bankAccountService = bankAccountService;
            this.moneyTransferService = moneyTransferService;
            this.mapper = mapper;
        }


        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return View("IndexGuest");
            }

            var userId = GetCurrentUserId();

            BankAccountIndexViewModel[] bankAccounts =
                (await bankAccountService.GetAllAccountsByUserIdAsync<BankAccountIndexServiceModel>(userId))
                .Select(mapper.Map<BankAccountIndexViewModel>)
                .ToArray();
            MoneyTransferListingDto[] moneyTransfers = (await moneyTransferService
                    .GetLast10MoneyTransfersForUserAsync<MoneyTransferListingServiceModel>(userId))
                .Select(mapper.Map<MoneyTransferListingDto>)
                .ToArray();

            HomeViewModel viewModel = new HomeViewModel
            {
                UserBankAccounts = bankAccounts,
                MoneyTransfers = moneyTransfers
            };

            return View(viewModel);
        }
    }
}