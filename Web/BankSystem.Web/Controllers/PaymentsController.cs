using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using BankSystem.Common;
using BankSystem.Common.Configuration;
using BankSystem.Services.BankAccount;
using BankSystem.Services.Models.BankAccount;
using BankSystem.Web.Infrastructure.Helpers;
using BankSystem.Web.Infrastructure.Helpers.GlobalTransferHelpers;
using BankSystem.Web.Infrastructure.Helpers.GlobalTransferHelpers.Models;
using BankSystem.Web.Models;
using BankSystem.Web.Models.BankAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BankSystem.Web.Controllers
{
    [Authorize]
    public class PaymentsController : BaseController
    {
        private const int CookieValidityInMinutes = 5;
        private const string PaymentDataCookie = "PaymentData";
        private readonly IBankAccountService bankAccountService;

        private readonly BankConfiguration bankConfiguration;
        private readonly IGlobalTransferHelper globalTransferHelper;
        private readonly IMapper mapper;

        public PaymentsController(
            IOptions<BankConfiguration> bankConfigurationOptions,
            IBankAccountService bankAccountService,
            IGlobalTransferHelper globalTransferHelper,
            IMapper mapper)
        {
            bankConfiguration = bankConfigurationOptions.Value;
            this.bankAccountService = bankAccountService;
            this.globalTransferHelper = globalTransferHelper;
            this.mapper = mapper;
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        [Route("/pay")]
        public IActionResult SetCookie(string data)
        {
            string decodedData;

            try
            {
                decodedData = DirectPaymentsHelper.DecodePaymentRequest(data);
            }
            catch
            {
                return BadRequest();
            }

            // set payment data cookie
            Response.Cookies.Append(PaymentDataCookie, decodedData,
                new CookieOptions
                {
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    MaxAge = TimeSpan.FromMinutes(CookieValidityInMinutes)
                });

            return RedirectToAction("Process");
        }

        [HttpGet]
        [Route("/pay")]
        public async Task<IActionResult> Process()
        {
            bool cookieExists = Request.Cookies.TryGetValue(PaymentDataCookie, out var data);

            if (!cookieExists)
            {
                return RedirectToHome();
            }

            try
            {
                dynamic paymentRequest =
                    DirectPaymentsHelper.ParsePaymentRequest(data, bankConfiguration.CentralApiPublicKey);
                if (paymentRequest == null)
                {
                    return BadRequest();
                }

                dynamic paymentInfo = DirectPaymentsHelper.GetPaymentInfo(paymentRequest);

                PaymentConfirmBindingModel model = new PaymentConfirmBindingModel
                {
                    Amount = paymentInfo.Amount,
                    Description = paymentInfo.Description,
                    DestinationBankName = paymentInfo.DestinationBankName,
                    DestinationBankCountry = paymentInfo.DestinationBankCountry,
                    DestinationBankAccountUniqueId = paymentInfo.DestinationBankAccountUniqueId,
                    RecipientName = paymentInfo.RecipientName,
                    OwnAccounts = await GetAllAccountsAsync(GetCurrentUserId()),
                    DataHash = DirectPaymentsHelper.Sha256Hash(data)
                };

                return View(model);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> PayAsync(PaymentConfirmBindingModel model)
        {
            bool cookieExists = Request.Cookies.TryGetValue(PaymentDataCookie, out var data);

            if (!ModelState.IsValid ||
                !cookieExists ||
                model.DataHash != DirectPaymentsHelper.Sha256Hash(data))
            {
                return PaymentFailed(NotificationMessages.PaymentStateInvalid);
            }

            BankAccountDetailsServiceModel account =
                await bankAccountService.GetByIdAsync<BankAccountDetailsServiceModel>(model.AccountId);
            if (account == null || account.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            try
            {
                // read and validate payment data
                dynamic paymentRequest =
                    DirectPaymentsHelper.ParsePaymentRequest(data, bankConfiguration.CentralApiPublicKey);

                if (paymentRequest == null)
                {
                    return PaymentFailed(NotificationMessages.PaymentStateInvalid);
                }

                dynamic paymentInfo = DirectPaymentsHelper.GetPaymentInfo(paymentRequest);

                string returnUrl = paymentRequest.ReturnUrl;

                // transfer money to destination account
                GlobalTransferDto serviceModel = new GlobalTransferDto
                {
                    Amount = paymentInfo.Amount,
                    Description = paymentInfo.Description,
                    DestinationBankName = paymentInfo.DestinationBankName,
                    DestinationBankCountry = paymentInfo.DestinationBankCountry,
                    DestinationBankSwiftCode = paymentInfo.DestinationBankSwiftCode,
                    DestinationBankAccountUniqueId = paymentInfo.DestinationBankAccountUniqueId,
                    RecipientName = paymentInfo.RecipientName,
                    SourceAccountId = model.AccountId
                };

                GlobalTransferResult result = await globalTransferHelper.TransferMoneyAsync(serviceModel);

                if (result != GlobalTransferResult.Succeeded)
                {
                    return PaymentFailed(result == GlobalTransferResult.InsufficientFunds
                        ? NotificationMessages.InsufficientFunds
                        : NotificationMessages.TryAgainLaterError);
                }

                // delete cookie to prevent accidental duplicate payments
                Response.Cookies.Delete(PaymentDataCookie);

                // return signed success response
                var response = DirectPaymentsHelper.GenerateSuccessResponse(paymentRequest,
                    bankConfiguration.Key);

                return Ok(new
                {
                    success = true,
                    returnUrl = HttpUtility.HtmlEncode(returnUrl),
                    data = response
                });
            }
            catch
            {
                return PaymentFailed(NotificationMessages.PaymentStateInvalid);
            }
        }

        private IActionResult PaymentFailed(string message)
        {
            return Ok(new
            {
                success = false,
                errorMessage = message
            });
        }

        private async Task<OwnBankAccountListingViewModel[]> GetAllAccountsAsync(string userId)
        {
            return (await bankAccountService
                    .GetAllAccountsByUserIdAsync<BankAccountIndexServiceModel>(userId))
                .Select(mapper.Map<OwnBankAccountListingViewModel>)
                .ToArray();
        }
    }
}