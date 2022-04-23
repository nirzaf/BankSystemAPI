using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using BankSystem.Data;
using BankSystem.Services.Models.Card;
using Microsoft.EntityFrameworkCore;

namespace BankSystem.Services.Card
{
    public class CardService : BaseService, ICardService
    {
        private readonly ICardHelper cardHelper;
        private readonly IMapper mapper;

        public CardService(BankSystemDbContext context, ICardHelper cardHelper, IMapper mapper)
            : base(context)
        {
            this.cardHelper = cardHelper;
            this.mapper = mapper;
        }

        public async Task<bool> CreateAsync(CardCreateServiceModel model)
        {
            if (!IsEntityStateValid(model) ||
                !Context.Users.Any(u => u.Id == model.UserId))
            {
                return false;
            }

            string generatedNumber;
            string generated3DigitSecurityCode;
            do
            {
                generatedNumber = cardHelper.Generate16DigitNumber();
                generated3DigitSecurityCode = cardHelper.Generate3DigitSecurityCode();
            } while (await Context.Cards.AnyAsync(a
                         => a.Number == generatedNumber && a.SecurityCode == generated3DigitSecurityCode));

            BankSystem.Models.Card dbModel = mapper.Map<BankSystem.Models.Card>(model);
            dbModel.Number = generatedNumber;
            dbModel.SecurityCode = generated3DigitSecurityCode;

            await Context.Cards.AddAsync(dbModel);
            await Context.SaveChangesAsync();

            return true;
        }

        public async Task<T> GetAsync<T>(
            string cardNumber,
            string cardExpiryDate,
            string cardSecurityCode,
            string cardOwner)
            where T : CardBaseServiceModel
        {
            return await Context
                .Cards
                .AsNoTracking()
                .Where(c =>
                    c.Name == cardOwner &&
                    c.Number == cardNumber &&
                    c.SecurityCode == cardSecurityCode &&
                    c.ExpiryDate == cardExpiryDate)
                .ProjectTo<T>(mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        public async Task<T> GetAsync<T>(string id)
            where T : CardBaseServiceModel
        {
            return await Context
                .Cards
                .AsNoTracking()
                .Where(c => c.Id == id)
                .ProjectTo<T>(mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        public async Task<int> GetCountOfAllCardsOwnedByUserAsync(string userId)
        {
            return await Context
                .Cards
                .CountAsync(c => c.UserId == userId);
        }

        public async Task<IEnumerable<T>> GetCardsAsync<T>(string userId, int pageIndex = 1, int count = int.MaxValue)
            where T : CardBaseServiceModel
        {
            return await Context
                .Cards
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Skip((pageIndex - 1) * count)
                .Take(count)
                .ProjectTo<T>(mapper.ConfigurationProvider)
                .ToArrayAsync();
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (id == null)
            {
                return false;
            }

            BankSystem.Models.Card card = await Context
                .Cards
                .Where(c => c.Id == id)
                .SingleOrDefaultAsync();

            if (card == null)
            {
                return false;
            }

            Context.Cards.Remove(card);
            await Context.SaveChangesAsync();

            return true;
        }
    }
}