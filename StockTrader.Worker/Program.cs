using Microsoft.EntityFrameworkCore;
using StockTrader.Data;
using StockTrader.Data.Repositories;
using StockTrader.Service.Interfaces;
using StockTrader.Service.Services;
using StockTrader.Worker; // For WorkerService and WorkerSettings
using Microsoft.Extensions.Configuration; // Required for IConfiguration
using Microsoft.Extensions.DependencyInjection; // Required for IServiceCollection extensions like AddDbContext, AddScoped, AddHostedService
using Microsoft.Extensions.Hosting; // Required for IHost, Host
using System.Linq; // Required for .Any()
using System; // Required for Console

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        // Bind WorkerSettings from appsettings.json
        services.Configure<WorkerSettings>(configuration.GetSection("WorkerSettings"));

        // Configure DbContext (similar to API project, choose one or make it configurable)
        // Option 1: In-Memory (for development/testing if preferred)
        // services.AddDbContext<StockTraderDbContext>(options =>
        //     options.UseInMemoryDatabase("StockTraderWorkerDb"));

        // Option 2: Using PostgreSQL/SQL Server based on appsettings.json
        var connectionString = configuration.GetConnectionString("StockTraderDb");

        // UNCOMMENT ONE OF THE FOLLOWING AFTER SETTING THE CONNECTION STRING
        // AND ENSURING THE RESPECTIVE EF CORE PROVIDER NUGET PACKAGE IS IN StockTrader.Data
        // if (string.IsNullOrEmpty(connectionString))
        // {
        //     Console.WriteLine("Connection string 'StockTraderDb' not found or empty. Using In-Memory database for Worker.");
        //     services.AddDbContext<StockTraderDbContext>(options =>
        //         options.UseInMemoryDatabase("StockTraderWorkerFallbackDb"));
        // }
        // else if (connectionString.ToLower().Contains("host=") || connectionString.ToLower().Contains("server=")) // Basic check for PostgreSQL
        // {
        //     Console.WriteLine("Worker using PostgreSQL database.");
        //     services.AddDbContext<StockTraderDbContext>(options =>
        //         options.UseNpgsql(connectionString));
        // }
        // else // Basic check for SQL Server
        // {
        //     Console.WriteLine("Worker using SQL Server database.");
        //     services.AddDbContext<StockTraderDbContext>(options =>
        //         options.UseSqlServer(connectionString));
        // }
        // FOR NOW, TO ENSURE IT RUNS WITHOUT USER CONFIG, WE WILL USE IN-MEMORY DIRECTLY.
        Console.WriteLine("Using In-Memory database for StockTrader.Worker by default. Configure ConnectionStrings in appsettings.json for a persistent database.");
        services.AddDbContext<StockTraderDbContext>(options =>
            options.UseInMemoryDatabase("StockTraderWorkerDefaultInMemoryDb"));


        // Register Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register Services needed by the worker
        services.AddScoped<IHistoricalDataService, HistoricalDataService>(); // May not be directly used by worker periodic tasks but good to have if seeding is moved here
        services.AddScoped<ILiveMarketDataService, LiveMarketDataService>();
        services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();
        services.AddScoped<ITradingStrategyEngine, TradingStrategyEngine>();
        services.AddScoped<ITradingTipService, TradingTipService>();
        
        // Seed initial stocks if needed, similar to API project, could be a shared seeding service
        // Ensure the Db is created, especially for In-Memory
        // Build a temporary ServiceProvider to create a scope for seeding.
        // This is a common pattern for seeding data at startup in a Hosted Service environment.
        var tempServiceProvider = services.BuildServiceProvider();
        using (var scope = tempServiceProvider.CreateScope()) 
        {
            var context = scope.ServiceProvider.GetRequiredService<StockTraderDbContext>();
            context.Database.EnsureCreated(); // Ensures DB is created, for In-Memory this is usually automatic but good practice.
             if (!context.Stocks.Any())
            {
                context.Stocks.AddRange(
                    new StockTrader.Core.Models.Stock { Symbol = "RELIANCE", Name = "Reliance Industries", Exchange = StockTrader.Core.Enums.Exchange.NSE },
                    new StockTrader.Core.Models.Stock { Symbol = "TCS", Name = "Tata Consultancy Services", Exchange = StockTrader.Core.Enums.Exchange.NSE },
                    new StockTrader.Core.Models.Stock { Symbol = "HDFCBANK", Name = "HDFC Bank", Exchange = StockTrader.Core.Enums.Exchange.BSE }
                );
                context.SaveChanges();
                Console.WriteLine("Worker Database seeded with initial stocks.");
            }
        }


        services.AddHostedService<WorkerService>();
    })
    .Build();

await host.RunAsync();
