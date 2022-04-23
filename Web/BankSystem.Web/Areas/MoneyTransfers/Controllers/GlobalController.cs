using BankSystem.Web.Models.BankAccount;

namespace BankSystem.Web.Areas.MoneyTransfers.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper;
    using Common;
    using Infrastructure.Helpers.GlobalTransferHelpers;
    using Infrastructure.Helpers.GlobalTransferHelpers.Models;
    using Microsoft.AspNetCore.Mvc;
    using Models.Global.Create;
    using Services.BankAccount;
    using Services.Models.BankAccount;
    using Services.User;

    public class GlobalController : BaseMoneyTransferController
    {
        private readonly IBankAccountService bankAccountService;
        private readonly IGlobalTransferHelper globalTransferHelper;
        private readonly IUserService userService;

        public GlobalController(
            IBankAccountService bankAccountService,
            IUserService userService,
            IGlobalTransferHelper globalTransferHelper,
            IMapper mapper)
            : base(bankAccountService, mapper)
        {
            this.bankAccountService = bankAccountService;
            this.userService = userService;
            this.globalTransferHelper = globalTransferHelper;
        }

        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();
            OwnBankAccountListingViewModel[] userAccounts = await GetAllAccountsAsync(userId);
            if (!userAccounts.Any())
            {
                ShowErrorMessage(NotificationMessages.NoAccountsError);

                return RedirectToHome();
            }

            GlobalMoneyTransferCreateBindingModel model = new GlobalMoneyTransferCreateBindingModel
            {
                OwnAccounts = userAccounts,
                SenderName = await userService.GetAccountOwnerFullnameAsync(userId)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(GlobalMoneyTransferCreateBindingModel model)
        {
            var userId = GetCurrentUserId();
            model.SenderName = await userService.GetAccountOwnerFullnameAsync(userId);
            if (!TryValidateModel(model))
            {
                model.OwnAccounts = await GetAllAccountsAsync(userId);

                return View(model);
            }

            BankAccountDetailsServiceModel account =
                await bankAccountService.GetByIdAsync<BankAccountDetailsServiceModel>(model.AccountId);
            if (account == null || account.UserId != userId)
            {
                return Forbid();
            }

            if (string.Equals(account.UniqueId, model.DestinationBank.Account.UniqueId,
                StringComparison.InvariantCulture))
            {
                ShowErrorMessage(NotificationMessages.SameAccountsError);
                model.OwnAccounts = await GetAllAccountsAsync(userId);

                return View(model);
            }

            GlobalTransferDto serviceModel = Mapper.Map<GlobalTransferDto>(model);
            serviceModel.SourceAccountId = model.AccountId;
            serviceModel.RecipientName = model.DestinationBank.Account.UserFullName;

            GlobalTransferResult result = await globalTransferHelper.TransferMoneyAsync(serviceModel);
            if (result != GlobalTransferResult.Succeeded)
            {
                if (result == GlobalTransferResult.InsufficientFunds)
                {
                    ShowErrorMessage(NotificationMessages.InsufficientFunds);
                    model.OwnAccounts = await GetAllAccountsAsync(userId);

                    return View(model);
                }

                ShowErrorMessage(NotificationMessages.TryAgainLaterError);
                return RedirectToHome();
            }

            ShowSuccessMessage(NotificationMessages.SuccessfulMoneyTransfer);
            return RedirectToHome();
        }
    }
}