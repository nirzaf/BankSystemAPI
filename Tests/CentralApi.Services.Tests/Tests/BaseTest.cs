﻿namespace CentralApi.Services.Tests.Tests
{
    using System;
    using AutoMapper;
    using Data;
    using Microsoft.EntityFrameworkCore;
    using Setup;

    public abstract class BaseTest
    {
        protected BaseTest()
            => Mapper = TestSetup.InitializeMapper();

        protected IMapper Mapper { get; }

        protected CentralApiDbContext DatabaseInstance
        {
            get
            {
                DbContextOptions<CentralApiDbContext> options = new DbContextOptionsBuilder<CentralApiDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .EnableSensitiveDataLogging()
                    .Options;

                return new CentralApiDbContext(options);
            }
        }
    }
}