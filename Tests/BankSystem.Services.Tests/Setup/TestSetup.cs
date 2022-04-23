namespace BankSystem.Services.Tests.Setup
{
    using AutoMapper;
    using Common.AutoMapping.Profiles;

    public static class TestSetup
    {
        private static IMapper _mapper;
        private static readonly object Sync = new object();
        private static bool _mapperInitialized;

        public static IMapper InitializeMapper()
        {
            lock (Sync)
            {
                if (_mapperInitialized)
                {
                    return _mapper;
                }

                var config = new MapperConfiguration(cfg => { cfg.AddProfile<DefaultProfile>(); });

                _mapper = config.CreateMapper();
                _mapperInitialized = true;

                return _mapper;
            }
        }
    }
}