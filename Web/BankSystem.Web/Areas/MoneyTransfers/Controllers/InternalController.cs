using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BankSystem.Common;
using BankSystem.Services.BankAccount;
using BankSystem.Services.Models.BankAccount;
using BankSystem.Services.Models.MoneyTransfer;
using BankSystem.Services.MoneyTransfer;
using BankSystem.Web.Areas.MoneyTransfers.Models.Internal;
using BankSystem.Web.Infrastructure;
using BankSystem.Web.Models.BankAccount;
using Microsoft.AspNetCore.Mvc;

namespace BankSystem.Web.Areas.MoneyTransfers.Controllers
{
    public class InternalController : BaseMoneyTransferController
    {
        private readonly IBankAccountService bankAccountService;
        private readonly IMoneyTransferService moneyTransferService;

        public InternalController(
            IMoneyTransferService moneyTransferService,
            IBankAccountService bankAccountService,
            IMapper mapper)
            : base(bankAccountService, mapper)
        {
            this.moneyTransferService = moneyTransferService;
            this.bankAccountService = bankAccountService;
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

            InternalMoneyTransferCreateBindingModel model = new InternalMoneyTransferCreateBindingModel
            {
                OwnAccounts = userAccounts
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(InternalMoneyTransferCreateBindingModel model)
        {
            var userId = GetCurrentUserId();

            if (!ModelState.IsValid)
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

            if (string.Equals(account.UniqueId, model.DestinationBankAccountUniqueId,
                    StringComparison.InvariantCulture))
            {
                ShowErrorMessage(NotificationMessages.SameAccountsError);
                model.OwnAccounts = await GetAllAccountsAsync(userId);

                return View(model);
            }

            if (account.Balance < model.Amount)
            {
                ShowErrorMessage(NotificationMessages.InsufficientFunds);
                model.OwnAccounts = await GetAllAccountsAsync(userId);

                return View(model);
            }

            BankAccountConciseServiceModel destinationAccount =
                await bankAccountService.GetByUniqueIdAsync<BankAccountConciseServiceModel>(
                    model.DestinationBankAccountUniqueId);
            if (destinationAccount == null)
            {
                ShowErrorMessage(NotificationMessages.DestinationBankAccountDoesNotExist);
                model.OwnAccounts = await GetAllAccountsAsync(userId);

                return View(model);
            }

            var referenceNumber = ReferenceNumberGenerator.GenerateReferenceNumber();
            MoneyTransferCreateServiceModel sourceServiceModel = Mapper.Map<MoneyTransferCreateServiceModel>(model);
            sourceServiceModel.Source = account.UniqueId;
            sourceServiceModel.Amount *= -1;
            sourceServiceModel.SenderName = account.UserFullName;
            sourceServiceModel.RecipientName = destinationAccount.UserFullName;
            sourceServiceModel.ReferenceNumber = referenceNumber;

            if (!await moneyTransferService.CreateMoneyTransferAsync(sourceServiceModel))
            {
                ShowErrorMessage(NotificationMessages.TryAgainLaterError);
                model.OwnAccounts = await GetAllAccountsAsync(userId);

                return View(model);
            }

            MoneyTransferCreateServiceModel destinationServiceModel =
                Mapper.Map<MoneyTransferCreateServiceModel>(model);
            destinationServiceModel.Source = account.UniqueId;
            destinationServiceModel.AccountId = destinationAccount.Id;
            destinationServiceModel.SenderName = account.UserFullName;
            destinationServiceModel.RecipientName = destinationAccount.UserFullName;
            destinationServiceModel.ReferenceNumber = referenceNumber;

            if (!await moneyTransferService.CreateMoneyTransferAsync(destinationServiceModel))
            {
                ShowErrorMessage(NotificationMessages.TryAgainLaterError);
                model.OwnAccounts = await GetAllAccountsAsync(userId);

                return View(model);
            }

            ShowSuccessMessage(NotificationMessages.SuccessfulMoneyTransfer);

            return RedirectToHome();
        }
    }
}