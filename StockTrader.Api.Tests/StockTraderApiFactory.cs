using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockTrader.Data; // For StockTraderDbContext
using StockTrader.Core.Models; // For Stock model for seeding
using StockTrader.Core.Enums; // For Exchange enum
using System;
using System.Linq;

namespace StockTrader.Api.Tests;

public class StockTraderApiFactory : WebApplicationFactory<Program> // 'Program' refers to the Program.cs of StockTrader.Api
{
    // Using a unique database name for each factory instance ensures test isolation at the factory level.
    // For stricter isolation (per test method), DbContext could be configured in each test,
    // but this is a common and often sufficient approach.
    private readonly string _databaseName = $"InMemoryStockTraderTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 1. Remove the app's StockTraderDbContext registration.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<StockTraderDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // 2. Add StockTraderDbContext using an In-Memory database for testing.
            services.AddDbContext<StockTraderDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // 3. (Optional) Add any other test-specific service overrides here.
            // For example, if you wanted to replace IMyService with MockMyService:
            // services.AddScoped<IMyService, MockMyService>();

            // 4. Build a temporary service provider to access services (like DbContext for seeding).
            var sp = services.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<StockTraderDbContext>();
                var logger = scopedServices.GetRequiredService<ILogger<StockTraderApiFactory>>();

                try
                {
                    // Ensure the database is created.
                    db.Database.EnsureCreated();

                    // Seed data for testing
                    SeedDatabase(db);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred seeding the test database. Error: {Message}", ex.Message);
                }
            }
        });

        builder.UseEnvironment("Development"); // Or "Testing" if you have specific test configurations
    }

    private static void SeedDatabase(StockTraderDbContext context)
    {
        // Add some initial data that can be relied upon by tests.
        if (!context.Stocks.Any())
        {
            context.Stocks.AddRange(
                new Stock { Symbol = "TESTMSFT", Name = "Microsoft Test Corp", Exchange = Exchange.NSE, Id = 101 },
                new Stock { Symbol = "TESTAAPL", Name = "Apple Test Inc", Exchange = Exchange.NSE, Id = 102 },
                new Stock { Symbol = "TESTGOOG", Name = "Google Test LLC", Exchange = Exchange.BSE, Id = 103 }
            );
            // Do not give explicit Ids if the DB is generating them.
            // For In-Memory, explicit IDs are fine if you manage them.
            // If Id is auto-generated (IDENTITY), remove Id from here.
            // Let's assume Id is NOT auto-generated for these specific test seeds for predictability.
        }

        if(!context.HistoricalPrices.Any())
        {
            context.HistoricalPrices.AddRange(
                new HistoricalPrice { StockId = 101, Date = DateTime.UtcNow.AddDays(-1).Date, Open=100, High=102, Low=99, Close=101, Volume=10000 },
                new HistoricalPrice { StockId = 101, Date = DateTime.UtcNow.AddDays(-2).Date, Open=98, High=100, Low=97, Close=100, Volume=12000 }
            );
        }
        
        if(!context.TradingTips.Any())
        {
            context.TradingTips.AddRange(
                new TradingTip { StockSymbol = "TESTMSFT", Action = ActionType.Buy, TipType = TipType.Intraday, Reason="Test Buy Tip", Timestamp = DateTime.UtcNow.AddHours(-1), ConfidenceScore = 0.7f, Id = 201},
                new TradingTip { StockSymbol = "TESTAAPL", Action = ActionType.Sell, TipType = TipType.Swing, Reason="Test Sell Tip", Timestamp = DateTime.UtcNow.AddHours(-2), ConfidenceScore = 0.6f, Id = 202}
            );
        }

        context.SaveChanges();
    }
}
