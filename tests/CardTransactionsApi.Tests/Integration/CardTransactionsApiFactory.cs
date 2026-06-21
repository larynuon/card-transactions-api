using CardTransactionsApi.Data;
using CardTransactionsApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CardTransactionsApi.Tests.Integration;

public sealed class CardTransactionsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IExchangeRateService>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"card-transactions-{Guid.NewGuid()}"));

            services.AddSingleton<IExchangeRateService, FakeExchangeRateService>();
        });
    }
}
