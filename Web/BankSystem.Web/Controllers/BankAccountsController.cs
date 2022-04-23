using BankSystem.Web.Infrastructure.Collections;

namespace BankSystem.Web.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Areas.MoneyTransfers.Models;
    using AutoMapper;
    using Common;
    using Common.Configuration;
    using Infrastructure.Extensions;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Models.BankAccount;
    using Services.BankAccount;
    using Services.Models.BankAccount;
    using Services.Models.MoneyTransfer;
    using Services.MoneyTransfer;

    public class BankAccountsController : BaseController
    {
        private const int ItemsPerPage = 10;

        private readonly IBankAccountService bankAccountService;
        private readonly BankConfiguration bankConfiguration;
        private readonly IMoneyTransferService moneyTransferService;
        private readonly IMapper mapper;

        public BankAccountsController(
            IBankAccountService bankAccountService,
            IMoneyTransferService moneyTransferService,
            IOptions<BankConfiguration> bankConfigurationOptions,
            IMapper mapper)
        {
            this.bankAccountService = bankAccountService;
            this.moneyTransferService = moneyTransferService;
            this.mapper = mapper;
            bankConfiguration = bankConfigurationOptions.Value;
        }

        public IActionResult Create()
            => View();

        [HttpPost]
        public async Task<IActionResult> Create(BankAccountCreateBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            BankAccountCreateServiceModel serviceModel = mapper.Map<BankAccountCreateServiceModel>(model);
            serviceModel.UserId = GetCurrentUserId();

            var accountId = await bankAccountService.CreateAsync(serviceModel);
            if (accountId == null)
            {
                ShowErrorMessage(NotificationMessages.BankAccountCreateError);

                return View(model);
            }

            ShowSuccessMessage(NotificationMessages.BankAccountCreated);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Details(string id, int pageIndex = 1)
        {
            pageIndex = Math.Max(1, pageIndex);

            BankAccountDetailsServiceModel account = await bankAccountService.GetByIdAsync<BankAccountDetailsServiceModel>(id);
            if (account == null ||
                account.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            var allTransfersCount = await moneyTransferService.GetCountOfAllMoneyTransfersForAccountAsync(id);
            PaginatedList<MoneyTransferListingDto> transfers = (await moneyTransferService
                    .GetMoneyTransfersForAccountAsync<MoneyTransferListingServiceModel>(id, pageIndex, ItemsPerPage))
                .Select(mapper.Map<MoneyTransferListingDto>)
                .ToPaginatedList(allTransfersCount, pageIndex, ItemsPerPage);

            BankAccountDetailsViewModel viewModel = mapper.Map<BankAccountDetailsViewModel>(account);
            viewModel.MoneyTransfers = transfers;
            viewModel.MoneyTransfersCount = allTransfersCount;
            viewModel.BankName = bankConfiguration.BankName;
            viewModel.BankCode = bankConfiguration.UniqueIdentifier;
            viewModel.BankCountry = bankConfiguration.Country;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeAccountNameAsync(string accountId, string name)
        {
            if (name == null)
            {
                return Ok(new
                {
                    success = false
                });
            }

            BankAccountDetailsServiceModel account = await bankAccountService.GetByIdAsync<BankAccountDetailsServiceModel>(accountId);
            if (account == null ||
                account.UserId != GetCurrentUserId())
            {
                return Ok(new
                {
                    success = false
                });
            }

            bool isSuccessful = await bankAccountService.ChangeAccountNameAsync(accountId, name);

            return Ok(new
            {
                success = isSuccessful
            });
        }

        public IActionResult ChangeAccountNameAsync()
        {
            throw new NotImplementedException();
        }
    }
}