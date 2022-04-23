﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace CentralApi.Infrastructure.Filters
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BankSystem.Common.Utils;
    using BankSystem.Common.Utils.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;
    using Services.Bank;
    using Services.Models.Banks;

    public class DecryptAndVerifyRequest : ActionFilterAttribute
    {
        private CentralApiConfiguration configuration;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var isValid = IsValidRequest(context)
                .GetAwaiter()
                .GetResult();

            if (!isValid)
            {
                context.Result = new ForbidResult();

                return;
            }

            base.OnActionExecuting(context);
        }

        private async Task<bool> IsValidRequest(
            ActionExecutingContext context)
        {
            HttpRequest request = context.HttpContext.Request;
            IBanksService bankService = request.HttpContext.RequestServices.GetService<IBanksService>();
            IOptions<CentralApiConfiguration> configOptions = request.HttpContext.RequestServices
                .GetService<IOptions<CentralApiConfiguration>>();
            configuration = configOptions.Value;

            IDictionary<string, object?> actionArguments = context.ActionArguments;
            var model = actionArguments.Values.First();

            if (model == null)
            {
                return false;
            }

            try
            {
                var incomingData = Encoding.UTF8.GetString(Convert.FromBase64String(model.ToString()));
                dynamic deserializedData = JsonConvert.DeserializeObject(incomingData);
                string bankName = deserializedData.BankName;
                string bankSwiftCode = deserializedData.BankSwiftCode;
                string bankCountry = deserializedData.BankCountry;

                BankServiceModel bank = await bankService.GetBankAsync<BankServiceModel>(bankName, bankSwiftCode, bankCountry);
                if (bank == null)
                {
                    return false;
                }

                string encryptedKey = deserializedData.EncryptedKey;
                string encryptedIv = deserializedData.EncryptedIv;
                string data = deserializedData.Data;
                string signature = deserializedData.Signature;

                var decryptedData = SignatureVerificationUtil
                    .DecryptDataAndVerifySignature(new SignatureVerificationModel
                    {
                        DecryptionPrivateKey = configuration.Key,
                        SignaturePublicKey = bank.ApiKey,
                        EncryptedKey = encryptedKey,
                        EncryptedIv = encryptedIv,
                        Data = data,
                        Signature = signature
                    });

                if (decryptedData == null)
                {
                    return false;
                }

                // Modify body
                var key = actionArguments.Keys.First();
                actionArguments.Remove(key);
                actionArguments.Add(key, decryptedData);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}