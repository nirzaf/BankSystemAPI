using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using CentralApi.Data;
using CentralApi.Services.Models.Banks;
using Microsoft.EntityFrameworkCore;

namespace CentralApi.Services.Bank
{
    public class BanksService : BaseService, IBanksService
    {
        private readonly IMapper mapper;
        
        public BanksService(CentralApiDbContext context, IMapper mapper)
            : base(context)
            => this.mapper = mapper;

        public async Task<T> GetBankAsync<T>(string bankName, string swiftCode, string bankCountry)
            where T : BankBaseServiceModel
        {
            const string likeExpression = "%{0}%";
            T bank = await Context
                .Banks
                .Where(b =>
                    EF.Functions.Like(b.Name, string.Format(likeExpression, bankName)) &&
                    EF.Functions.Like(b.SwiftCode, string.Format(likeExpression, swiftCode)) &&
                    EF.Functions.Like(b.Location, string.Format(likeExpression, bankCountry)))
                .ProjectTo<T>(mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();

            return bank;
        }

        public async Task<IEnumerable<T>> GetAllBanksSupportingPaymentsAsync<T>()
            where T : BankBaseServiceModel
        {
            T[] banks = await Context
                .Banks
                .AsNoTracking()
                .Where(b => b.PaymentUrl != null)
                .OrderBy(b => b.Location)
                .ThenBy(b => b.Name)
                .ProjectTo<T>(mapper.ConfigurationProvider)
                .ToArrayAsync();

            return banks;
        }

        public async Task<T> GetBankByIdAsync<T>(string id)
            where T : BankBaseServiceModel
        {
            return await Context
                .Banks
                .AsNoTracking()
                .Where(b => b.Id == id)
                .ProjectTo<T>(mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        public async Task<T> GetBankByBankIdentificationCardNumbersAsync<T>(string identificationCardNumbers)
            where T : BankBaseServiceModel
            => await Context
                .Banks
                .AsNoTracking()
                .Where(b => b.BankIdentificationCardNumbers == identificationCardNumbers)
                .ProjectTo<T>(mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
    }
}