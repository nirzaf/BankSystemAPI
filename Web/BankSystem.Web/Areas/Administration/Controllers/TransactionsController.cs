using System.Collections.Generic;

namespace BankSystem.Web.Areas.Administration.Controllers
{
    using AutoMapper;
    using Microsoft.AspNetCore.Mvc;
    using MoneyTransfers.Models;
    using Services.Models.MoneyTransfer;
    using System.Linq;
    using System.Threading.Tasks;
    using Models;
    using Services.MoneyTransfer;

    public class TransactionsController : BaseAdministrationController
    {
        private readonly IMoneyTransferService moneyTransfer;
        private readonly IMapper mapper;

        public TransactionsController(IMoneyTransferService moneyTransfer, IMapper mapper)
        {
            this.moneyTransfer = moneyTransfer;
            this.mapper = mapper;
        }

        public IActionResult Search() => View();

        public async Task<IActionResult> Result(string referenceNumber)
        {
            if (referenceNumber == null)
            {
                return NotFound();
            }

            IEnumerable<MoneyTransferListingDto> moneyTransfers = (await moneyTransfer
                    .GetMoneyTransferAsync<MoneyTransferListingServiceModel>(referenceNumber))
                .Select(mapper.Map<MoneyTransferListingDto>);

            TransactionListingViewModel viewModel = new TransactionListingViewModel
            {
                MoneyTransfers = moneyTransfers,
                ReferenceNumber = referenceNumber
            };

            return View(viewModel);
        }
    }
}