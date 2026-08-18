using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;
using Candoumbe.Types.Numerics;
using Documents.DataStores;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Documents.API;

/// <summary>
/// Provide extension methods used to configure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>
    /// <param name="services"></param>
    /// </summary>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds required dependencies to access API datastore
        /// </summary>
        public void AddDataStores()
        {
            using IServiceScope scope = services.BuildServiceProvider().CreateScope();

            services.AddTransient(serviceProvider =>
            {
                DbContextOptions<DocumentsStore> dbContextOptions = serviceProvider.GetRequiredService<DbContextOptions<DocumentsStore>>();
                IClock clock = serviceProvider.GetRequiredService<IClock>();
                return new DocumentsStore(dbContextOptions, clock);
            });

            services.AddSingleton<IUnitOfWorkFactory, EntityFrameworkUnitOfWorkFactory<DocumentsStore>>(serviceProvider =>
            {
                DbContextOptions<DocumentsStore> dbContextOptions = serviceProvider.GetRequiredService<DbContextOptions<DocumentsStore>>();

                IClock clock = serviceProvider.GetRequiredService<IClock>();
                return new EntityFrameworkUnitOfWorkFactory<DocumentsStore>(dbContextOptions, options => new DocumentsStore(options, clock), new DocumentRepositoryFactory());
            });

            return;

        }

        /// <summary>
        /// Adds supports for Options
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public IServiceCollection AddCustomOptions(IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<DocumentsApiOptions>(options =>
            {
                options.DefaultPageSize = PositiveInteger.From(configuration.GetValue($"ApiOptions:{nameof(DocumentsApiOptions.DefaultPageSize)}", 30));
                options.MaxPageSize = PositiveInteger.From(configuration.GetValue($"ApiOptions:{nameof(DocumentsApiOptions.DefaultPageSize)}", 100));
            });

            services.Configure<JwtOptions>(options =>
            {
                options.Issuer = configuration.GetValue<string>($"Authentication:{nameof(JwtOptions)}:{nameof(JwtOptions.Issuer)}");
                options.Audience = configuration.GetValue<string>($"Authentication:{nameof(JwtOptions)}:{nameof(JwtOptions.Audience)}");
                options.Key = configuration.GetValue<string>($"Authentication:{nameof(JwtOptions)}:{nameof(JwtOptions.Key)}");
            });

            return services;
        }

        /// <summary>
        /// Configure dependency injection container
        /// </summary>
        /// <remarks>
        /// Adds the
        /// </remarks>
        public void AddCustomizedDependencyInjection()
        {
            services.AddSingleton<IClock>(SystemClock.Instance);
            services.AddHttpContextAccessor();
            services.AddSingleton<IDocumentContentStorage, MinioDocumentContentStorage>();
        }
    }
}