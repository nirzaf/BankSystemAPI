using BankSystem.Web.Infrastructure.Collections;

namespace BankSystem.Web.Areas.MoneyTransfers.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper;
    using Infrastructure.Extensions;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Services.BankAccount;
    using Services.Models.MoneyTransfer;
    using Services.MoneyTransfer;

    public class HomeController : BaseMoneyTransferController
    {
        private const int PaymentsCountPerPage = 10;

        private readonly IMoneyTransferService moneyTransferService;
        
        public HomeController(
            IBankAccountService bankAccountService,
            IMoneyTransferService moneyTransferService,
            IMapper mapper)
            : base(bankAccountService, mapper)
        {
            this.moneyTransferService = moneyTransferService;
        }

        [Route("/{area}/Archives")]
        public async Task<IActionResult> All(int pageIndex = 1)
        {
            pageIndex = Math.Max(1, pageIndex);

            var userId = GetCurrentUserId();
            PaginatedList<MoneyTransferListingDto> allMoneyTransfers =
                (await moneyTransferService.GetMoneyTransfersAsync<MoneyTransferListingServiceModel>(userId,
                    pageIndex, PaymentsCountPerPage))
                .Select(Mapper.Map<MoneyTransferListingDto>)
                .ToPaginatedList(await moneyTransferService.GetCountOfAllMoneyTransfersForUserAsync(userId),
                    pageIndex, PaymentsCountPerPage);

            MoneyTransferListingViewModel transfers = new MoneyTransferListingViewModel
            {
                MoneyTransfers = allMoneyTransfers
            };

            return View(transfers);
        }
    }
}