﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace BankSystem.Web.Infrastructure.Filters
{
    using System;
    using System.Linq;
    using System.Text;
    using Common.Configuration;
    using Common.Utils;
    using Common.Utils.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Newtonsoft.Json;

    public class DecryptAndVerifyRequest : ActionFilterAttribute
    {
        private BankConfiguration configuration;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var isValid = IsValidRequest(context);

            if (!isValid)
            {
                context.Result = new ForbidResult();
                return;
            }

            base.OnActionExecuting(context);
        }

        private bool IsValidRequest(
            ActionExecutingContext context)
        {
            HttpRequest request = context.HttpContext.Request;
            IOptions<BankConfiguration> configOptions = request.HttpContext.RequestServices
                .GetService<IOptions<BankConfiguration>>();
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
                string encryptedKey = deserializedData.EncryptedKey;
                string encryptedIv = deserializedData.EncryptedIv;
                string data = deserializedData.Data;
                string signature = deserializedData.Signature;

                var decryptedData = SignatureVerificationUtil
                    .DecryptDataAndVerifySignature(new SignatureVerificationModel
                    {
                        DecryptionPrivateKey = configuration.Key,
                        SignaturePublicKey = configuration.CentralApiPublicKey,
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