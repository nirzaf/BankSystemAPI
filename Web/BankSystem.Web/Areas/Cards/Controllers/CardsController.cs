using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BankSystem.Common;
using BankSystem.Services.BankAccount;
using BankSystem.Services.Card;
using BankSystem.Services.Models.BankAccount;
using BankSystem.Services.Models.Card;
using BankSystem.Web.Areas.Cards.Models;
using BankSystem.Web.Infrastructure.Collections;
using BankSystem.Web.Infrastructure.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BankSystem.Web.Areas.Cards.Controllers
{
    public class CardsController : BaseCardsController
    {
        private const int CardsCountPerPage = 10;
        private readonly IBankAccountService bankAccountService;
        private readonly ICardService cardService;
        private readonly IMapper mapper;

        public CardsController(
            IBankAccountService bankAccountService,
            ICardService cardService,
            IMapper mapper)
        {
            this.bankAccountService = bankAccountService;
            this.cardService = cardService;
            this.mapper = mapper;
        }

        public async Task<IActionResult> Index(int pageIndex = 1)
        {
            pageIndex = Math.Max(1, pageIndex);

            var userId = GetCurrentUserId();
            PaginatedList<CardListingDto> allCards = (await cardService
                    .GetCardsAsync<CardDetailsServiceModel>(userId, pageIndex, CardsCountPerPage))
                .Select(mapper.Map<CardListingDto>)
                .ToPaginatedList(await cardService.GetCountOfAllCardsOwnedByUserAsync(userId), pageIndex,
                    CardsCountPerPage);

            CardListingViewModel cards = new CardListingViewModel
            {
                Cards = allCards
            };

            return View(cards);
        }

        public async Task<IActionResult> Create()
        {
            IEnumerable<SelectListItem> userAccounts = await GetAllAccountsAsync(GetCurrentUserId());

            CardCreateViewModel model = new CardCreateViewModel
            {
                BankAccounts = userAccounts
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CardCreateViewModel model)
        {
            var userId = GetCurrentUserId();
            if (!ModelState.IsValid)
            {
                model.BankAccounts = await GetAllAccountsAsync(userId);

                return View(model);
            }

            BankAccountDetailsServiceModel account =
                await bankAccountService.GetByIdAsync<BankAccountDetailsServiceModel>(model.AccountId);
            if (account == null || account.UserId != userId)
            {
                return Forbid();
            }

            CardCreateServiceModel serviceModel = mapper.Map<CardCreateServiceModel>(model);
            serviceModel.UserId = userId;
            serviceModel.Name = account.UserFullName;
            serviceModel.ExpiryDate = DateTime.UtcNow.AddYears(GlobalConstants.CardValidityInYears)
                .ToString(GlobalConstants.CardExpirationDateFormat, CultureInfo.InvariantCulture);

            bool isCreated = await cardService.CreateAsync(serviceModel);
            if (!isCreated)
            {
                ShowErrorMessage(NotificationMessages.CardCreateError);

                return RedirectToHome();
            }

            ShowSuccessMessage(NotificationMessages.CardCreatedSuccessfully);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                ShowErrorMessage(NotificationMessages.CardDoesNotExist);

                return RedirectToAction(nameof(Index));
            }

            CardDetailsServiceModel card = await cardService.GetAsync<CardDetailsServiceModel>(id);
            if (card == null || card.UserId != GetCurrentUserId())
            {
                ShowErrorMessage(NotificationMessages.CardDoesNotExist);

                return RedirectToAction(nameof(Index));
            }

            var isDeleted = await cardService.DeleteAsync(id);
            if (!isDeleted)
            {
                ShowErrorMessage(NotificationMessages.CardDeleteError);
            }
            else
            {
                ShowSuccessMessage(NotificationMessages.CardDeletedSuccessfully);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<IEnumerable<SelectListItem>> GetAllAccountsAsync(string userId)
        {
            return (await bankAccountService
                    .GetAllAccountsByUserIdAsync<BankAccountIndexServiceModel>(userId))
                .Select(a => new SelectListItem { Text = a.Name, Value = a.Id })
                .ToArray();
        }
    }
}