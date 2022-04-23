namespace BankSystem.Services.MoneyTransfer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper;
    using AutoMapper.QueryableExtensions;
    using BankSystem.Models;
    using Common;
    using Common.EmailSender.Interface;
    using Data;
    using Microsoft.EntityFrameworkCore;
    using Models.MoneyTransfer;

    public class MoneyTransferService : BaseService, IMoneyTransferService
    {
        private readonly IEmailSender emailSender;
        private readonly IMapper mapper;
        
        public MoneyTransferService(BankSystemDbContext context, IEmailSender emailSender, IMapper mapper)
            : base(context)
        {
            this.emailSender = emailSender;
            this.mapper = mapper;
        }

        public async Task<IEnumerable<T>> GetMoneyTransferAsync<T>(string referenceNumber)
            where T : MoneyTransferBaseServiceModel
            => await Context
                .Transfers
                .AsNoTracking()
                .Where(t => t.ReferenceNumber == referenceNumber)
                .ProjectTo<T>(mapper.ConfigurationProvider)
                .ToArrayAsync();

        public async Task<int> GetCountOfAllMoneyTransfersForUserAsync(string userId)
            => await Context
                .Transfers
                .CountAsync(t => t.Account.UserId == userId);

        public async Task<IEnumerable<T>> GetMoneyTransfersAsync<T>(string userId, int pageIndex = 1, int count = int.MaxValue)
            where T : MoneyTransferBaseServiceModel
            => await Context
                .Transfers
                .AsNoTracking()
                .Where(t => t.Account.UserId == userId)
                .OrderByDescending(mt => mt.MadeOn)
                .Skip((pageIndex - 1) * count)
                .Take(count)
                .ProjectTo<T>(mapper.ConfigurationProvider)
                .ToArrayAsync();

        public async Task<int> GetCountOfAllMoneyTransfersForAccountAsync(string accountId)
            => await Context
                .Transfers
                .CountAsync(t => t.AccountId == accountId);

        public async Task<IEnumerable<T>> GetMoneyTransfersForAccountAsync<T>(string accountId, int pageIndex = 1, int count = int.MaxValue)
            where T : MoneyTransferBaseServiceModel
            => await Context
                .Transfers
                .AsNoTracking()
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(mt => mt.MadeOn)
                .Skip((pageIndex - 1) * count)
                .Take(count)
                .ProjectTo<T>(mapper.ConfigurationProvider)
                .ToArrayAsync();

        public async Task<IEnumerable<T>> GetLast10MoneyTransfersForUserAsync<T>(string userId)
            where T : MoneyTransferBaseServiceModel
            => await Context
                .Transfers
                .AsNoTracking()
                .Where(mt => mt.Account.UserId == userId)
                .OrderByDescending(mt => mt.MadeOn)
                .Take(10)
                .ProjectTo<T>(mapper.ConfigurationProvider)
                .ToArrayAsync();

        public async Task<bool> CreateMoneyTransferAsync<T>(T model)
            where T : MoneyTransferBaseServiceModel
        {
            if (!IsEntityStateValid(model))
            {
                return false;
            }

            MoneyTransfer dbModel = mapper.Map<MoneyTransfer>(model);
            BankAccount userAccount = await Context
                .Accounts
                .Include(u => u.User)
                .Where(u => u.Id == dbModel.AccountId)
                .SingleOrDefaultAsync();
            if (userAccount == null)
            {
                return false;
            }

            userAccount.Balance += dbModel.Amount;
            Context.Update(userAccount);

            await Context.Transfers.AddAsync(dbModel);
            await Context.SaveChangesAsync();

            if (dbModel.Amount > 0)
            {
                await emailSender.SendEmailAsync(dbModel.Account.User.Email, EmailMessages.ReceiveMoneySubject,
                    string.Format(EmailMessages.ReceiveMoneyMessage, dbModel.Amount));
            }
            else
            {
                await emailSender.SendEmailAsync(dbModel.Account.User.Email, EmailMessages.SendMoneySubject,
                    string.Format(EmailMessages.SendMoneyMessage, Math.Abs(dbModel.Amount)));
            }

            return true;
        }
    }
}