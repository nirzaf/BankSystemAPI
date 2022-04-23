﻿namespace BankSystem.Web.Api
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using AutoMapper;
    using Common;
    using Infrastructure.Filters;
    using Infrastructure.Helpers.GlobalTransferHelpers;
    using Infrastructure.Helpers.GlobalTransferHelpers.Models;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Newtonsoft.Json;
    using Services.Card;
    using Services.Models.Card;

    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    [DecryptAndVerifyRequest]
    [ApiController]
    public class CardPaymentsController : ControllerBase
    {
        private readonly ICardService cardService;
        private readonly IGlobalTransferHelper globalTransferHelper;
        private readonly IMapper mapper;
        
        public CardPaymentsController(IGlobalTransferHelper globalTransferHelper, ICardService cardService, IMapper mapper)
        {
            this.globalTransferHelper = globalTransferHelper;
            this.cardService = cardService;
            this.mapper = mapper;
        }

        // POST: api/CardPayments
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] string data)
        {
            var model = JsonConvert.DeserializeObject<PaymentInfoModel>(data);
            if (TryValidateModel(model))
            {
                return BadRequest();
            }

            var card = await cardService.GetAsync<CardDetailsServiceModel>(
                model.Number,
                model.ExpiryDate,
                model.SecurityCode,
                model.Name);

            if (card == null)
            {
                return BadRequest();
            }

            bool expirationDateValid = DateTime.TryParseExact(
                card.ExpiryDate,
                GlobalConstants.CardExpirationDateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var expirationDate);

            if (!expirationDateValid || expirationDate.AddMonths(1) < DateTime.UtcNow)
            {
                return BadRequest();
            }

            var serviceModel = mapper.Map<GlobalTransferDto>(model);
            serviceModel.SourceAccountId = card.AccountId;

            var result = await globalTransferHelper.TransferMoneyAsync(serviceModel);

            if (result != GlobalTransferResult.Succeeded)
            {
                return BadRequest();
            }

            return Ok();
        }
    }
}