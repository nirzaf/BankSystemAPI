using System;
using System.Linq;
using AutoMapper;
using BankSystem.Common.AutoMapping.Interfaces;

namespace BankSystem.Common.AutoMapping.Profiles
{
    public class DefaultProfile : Profile
    {
        public DefaultProfile()
        {
            ConfigureMapping();
        }

        private void ConfigureMapping()
        {
            //TODO: Refactor, this is really bad to have this as a constant,
            // CentralApi should be extracted in separate sln file or at least it shouldn't share logic with BankSystem.Common
            // If we don't specify exactly the assemblies we want our tests might fail because not all assemblies are loaded on start up
            // since assemblies in .NET are lazy loaded
            const string centralApiNamespace = "CentralApi";
            Type[] allTypes = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(a => a.GetName().FullName.Contains(nameof(BankSystem)) ||
                            a.GetName().FullName.Contains(centralApiNamespace))
                .SelectMany(a => a.GetTypes())
                .ToArray();

            var withBidirectionalMapping = allTypes
                .Where(t => t.IsClass
                            && !t.IsAbstract
                            && t.GetInterfaces()
                                .Where(i => i.IsGenericType)
                                .Select(i => i.GetGenericTypeDefinition())
                                .Contains(typeof(IMapWith<>)))
                .SelectMany(t =>
                    t.GetInterfaces()
                        .Where(i => i.IsGenericType &&
                                    i.GetGenericTypeDefinition() == typeof(IMapWith<>))
                        .SelectMany(i => i.GetGenericArguments())
                        .Select(s => new
                        {
                            Type1 = t,
                            Type2 = s
                        })
                )
                .ToArray();

            //Create bidirectional mapping for all types implementing the IMapWith<TModel> interface
            foreach (var mapping in withBidirectionalMapping)
            {
                CreateMap(mapping.Type1, mapping.Type2);
                CreateMap(mapping.Type2, mapping.Type1);
            }

            // Create custom mapping for all types implementing the IHaveCustomMapping interface
            IHaveCustomMapping[] customMappings = allTypes.Where(t => t.IsClass
                                                                      && !t.IsAbstract
                                                                      && typeof(IHaveCustomMapping).IsAssignableFrom(t))
                .Select(Activator.CreateInstance)
                .Cast<IHaveCustomMapping>()
                .ToArray();

            foreach (IHaveCustomMapping mapping in customMappings)
            {
                mapping.ConfigureMapping(this);
            }
        }
    }
}