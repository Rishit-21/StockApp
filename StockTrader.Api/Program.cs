using Microsoft.EntityFrameworkCore;
using StockTrader.Core.Models; // If any core models are directly used by services for configuration
using StockTrader.Data;
using StockTrader.Data.Repositories;
using StockTrader.Service.Interfaces;
using StockTrader.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure DbContext
// Option 1: In-Memory (for development/testing if preferred, but plan mentions connection string)
// builder.Services.AddDbContext<StockTraderDbContext>(options =>
//     options.UseInMemoryDatabase("StockTraderApiDb"));

// Option 2: Using PostgreSQL/SQL Server based on appsettings.json
// IMPORTANT: The user needs to replace the placeholder in appsettings.json
var connectionString = builder.Configuration.GetConnectionString("StockTraderDb");

// UNCOMMENT ONE OF THE FOLLOWING AFTER SETTING THE CONNECTION STRING
// AND ENSURING THE RESPECTIVE EF CORE PROVIDER NUGET PACKAGE IS IN StockTrader.Data
// if (string.IsNullOrEmpty(connectionString))
// {
//     // Fallback to In-Memory if connection string is not set
//     Console.WriteLine("Connection string 'StockTraderDb' not found or empty. Using In-Memory database.");
//     builder.Services.AddDbContext<StockTraderDbContext>(options =>
//         options.UseInMemoryDatabase("StockTraderFallbackDb"));
// }
// else if (connectionString.ToLower().Contains("host=") || connectionString.ToLower().Contains("server=")) // Basic check for PostgreSQL
// {
//     Console.WriteLine("Using PostgreSQL database.");
//     builder.Services.AddDbContext<StockTraderDbContext>(options =>
//         options.UseNpgsql(connectionString));
// }
// else // Basic check for SQL Server
// {
//     Console.WriteLine("Using SQL Server database.");
//     builder.Services.AddDbContext<StockTraderDbContext>(options =>
//         options.UseSqlServer(connectionString));
// }
// FOR NOW, TO ENSURE IT RUNS WITHOUT USER CONFIG, WE WILL USE IN-MEMORY DIRECTLY.
// THE USER IS INSTRUCTED TO CHANGE THIS LATER.
Console.WriteLine("Using In-Memory database for StockTrader.Api by default. Configure ConnectionStrings in appsettings.json for a persistent database.");
builder.Services.AddDbContext<StockTraderDbContext>(options =>
     options.UseInMemoryDatabase("StockTraderApiDefaultInMemoryDb"));


// Register Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register Services
builder.Services.AddScoped<IHistoricalDataService, HistoricalDataService>();
builder.Services.AddScoped<ILiveMarketDataService, LiveMarketDataService>();
builder.Services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();
builder.Services.AddScoped<ITradingStrategyEngine, TradingStrategyEngine>(); // This line is now uncommented
builder.Services.AddScoped<ITradingTipService, TradingTipService>();
// Placeholder for a stock management service if needed for adding/listing stocks primarily through API
// builder.Services.AddScoped<IStockManagementService, StockManagementService>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Seed data for development (optional, can also be done via a dedicated service or migrations)
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<StockTraderDbContext>();
            // Ensure database is created. For In-Memory, this is usually automatic.
            // For real DBs, context.Database.Migrate() might be called here if using migrations.
            context.Database.EnsureCreated(); // Good for In-Memory and code-first without migrations

            // Seed initial stock data if database is empty
            if (!context.Stocks.Any())
            {
                context.Stocks.AddRange(
                    new Stock { Symbol = "RELIANCE", Name = "Reliance Industries", Exchange = StockTrader.Core.Enums.Exchange.NSE },
                    new Stock { Symbol = "TCS", Name = "Tata Consultancy Services", Exchange = StockTrader.Core.Enums.Exchange.NSE },
                    new Stock { Symbol = "HDFCBANK", Name = "HDFC Bank", Exchange = StockTrader.Core.Enums.Exchange.BSE }
                );
                context.SaveChanges();
                Console.WriteLine("Database seeded with initial stocks.");
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred seeding the DB.");
        }
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
