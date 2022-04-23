using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CentralApi.Infrastructure.Helpers.PaymentHelpers;
using CentralApi.Models;
using CentralApi.Services.Bank;
using CentralApi.Services.Models.Banks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CentralApi.Controllers
{
    public class PaymentsController : Controller
    {
        private const int CookieValidityInMinutes = 5;
        private const string PaymentDataCookie = "PaymentData";
        private const string PaymentDataFormKey = "data";

        private readonly IBanksService banksService;
        private readonly CentralApiConfiguration configuration;
        private readonly IMapper mapper;

        public PaymentsController(
            IBanksService banksService,
            IMapper mapper,
            IOptions<CentralApiConfiguration> configuration)
        {
            this.banksService = banksService;
            this.mapper = mapper;
            this.configuration = configuration.Value;
        }


        [HttpPost]
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
                    HttpOnly = true,
                    IsEssential = true,
                    MaxAge = TimeSpan.FromMinutes(CookieValidityInMinutes)
                });

            return RedirectToAction("Process");
        }

        [HttpGet]
        [Route("/pay")]
        public async Task<IActionResult> Process()
        {
            bool cookieExists = Request.Cookies.TryGetValue(PaymentDataCookie, out string data);

            if (!cookieExists)
            {
                return BadRequest();
            }

            try
            {
                var request = DirectPaymentsHelper.ParsePaymentRequest(data);

                if (request == null)
                {
                    return BadRequest();
                }

                var paymentInfo = DirectPaymentsHelper.GetPaymentInfo(request);

                BankListingViewModel[] banks =
                    (await banksService.GetAllBanksSupportingPaymentsAsync<BankListingServiceModel>())
                    .Select(mapper.Map<BankListingViewModel>)
                    .ToArray();

                PaymentSelectBankViewModel viewModel = new PaymentSelectBankViewModel
                {
                    Amount = paymentInfo.Amount,
                    Description = paymentInfo.Description,
                    Banks = banks
                };

                return View(viewModel);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("/pay/continue")]
        public async Task<IActionResult> Continue([FromForm] string bankId)
        {
            bool cookieExists = Request.Cookies.TryGetValue(PaymentDataCookie, out var data);

            if (!cookieExists)
            {
                return BadRequest();
            }

            try
            {
                var request = DirectPaymentsHelper.ParsePaymentRequest(data);

                if (request == null)
                {
                    return BadRequest();
                }

                BankPaymentServiceModel bank = await banksService.GetBankByIdAsync<BankPaymentServiceModel>(bankId);
                if (bank?.PaymentUrl == null)
                {
                    return BadRequest();
                }

                // generate PaymentProof containing the bank's public key
                // and merchant's original PaymentInfo signature
                string proofRequest = DirectPaymentsHelper.GeneratePaymentRequestWithProof(request,
                    bank.ApiKey, configuration.Key);

                // redirect the user to their bank for payment completion
                PaymentPostRedirectModel paymentPostRedirectModel = new PaymentPostRedirectModel
                {
                    Url = bank.PaymentUrl,
                    PaymentDataFormKey = PaymentDataFormKey,
                    PaymentData = proofRequest
                };

                return View("PaymentPostRedirect", paymentPostRedirectModel);
            }
            catch
            {
                return BadRequest();
            }
        }
    }
}