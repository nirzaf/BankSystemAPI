using BankSystem.Web.Infrastructure.Collections;

namespace BankSystem.Web.Areas.Administration.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper;
    using Infrastructure.Extensions;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Services.BankAccount;
    using Services.Models.BankAccount;

    public class AccountsController : BaseAdministrationController
    {
        private const int AccountsPerPage = 20;

        private readonly IBankAccountService bankAccountService;
        private readonly IMapper mapper;

        public AccountsController(IBankAccountService bankAccountService, IMapper mapper)
        {
            this.bankAccountService = bankAccountService;
            this.mapper = mapper;
        }

        public async Task<IActionResult> Index(int pageIndex = 1)
        {
            pageIndex = Math.Max(1, pageIndex);

            PaginatedList<BankAccountListingViewModel> allAccounts = (await bankAccountService.GetAccountsAsync<BankAccountDetailsServiceModel>(pageIndex, AccountsPerPage))
                .Select(mapper.Map<BankAccountListingViewModel>)
                .ToPaginatedList(await bankAccountService.GetCountOfAccountsAsync(), pageIndex, AccountsPerPage);

            AllBankAccountsListViewModel transfers = new AllBankAccountsListViewModel
            {
                BankAccounts = allAccounts
            };

            return View(transfers);
        }
    }
}