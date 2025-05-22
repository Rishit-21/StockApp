using Microsoft.EntityFrameworkCore;
using StockTrader.Core.Models; // Location of your domain models

namespace StockTrader.Data;

public class StockTraderDbContext : DbContext
{
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<HistoricalPrice> HistoricalPrices { get; set; }
    public DbSet<LivePrice> LivePrices { get; set; }
    public DbSet<SentimentData> SentimentData { get; set; }
    public DbSet<TradingTip> TradingTips { get; set; }

    // Constructor used by ASP.NET Core's dependency injection
    public StockTraderDbContext(DbContextOptions<StockTraderDbContext> options)
        : base(options)
    {
    }

    // Parameterless constructor for specific scenarios like local instantiation
    // or if you plan to use OnConfiguring for simpler setups outside DI.
    // However, for testing with InMemory or configuring for real DBs via DI,
    // the constructor above is primary.
    public StockTraderDbContext() {}


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Default to In-Memory database if no options are passed.
            // This is useful for unit tests or basic console app usage
            // where DI might not be set up for DbContext.
            optionsBuilder.UseInMemoryDatabase("StockTraderDb");

            // ----- HOW TO SWITCH TO A REAL DATABASE -----
            //
            // 1. Ensure you have the appropriate NuGet package installed:
            //    - For PostgreSQL: Npgsql.EntityFrameworkCore.PostgreSQL
            //    - For SQL Server: Microsoft.EntityFrameworkCore.SqlServer
            //
            // 2. Comment out or remove the .UseInMemoryDatabase() line above.
            //
            // 3. Uncomment one of the following sections and replace
            //    "YOUR_CONNECTION_STRING_HERE" with your actual connection string.
            //    This connection string would typically come from appsettings.json
            //    in your API or Worker projects and be passed via DbContextOptions
            //    through dependency injection. This OnConfiguring override (when
            //    !optionsBuilder.IsConfigured) is more of a fallback or for simpler setups.

            /*
            // --- PostgreSQL Configuration ---
            // string postgresConnectionString = "YOUR_CONNECTION_STRING_HERE";
            // optionsBuilder.UseNpgsql(postgresConnectionString);
            */

            /*
            // --- SQL Server Configuration ---
            // string sqlServerConnectionString = "YOUR_CONNECTION_STRING_HERE";
            // optionsBuilder.UseSqlServer(sqlServerConnectionString);
            */
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Example: Configure a composite key if necessary
        // modelBuilder.Entity<YourEntity>().HasKey(ye => new { ye.Key1, ye.Key2 });

        // Example: Configure relationships explicitly if conventions are not enough
        // modelBuilder.Entity<Stock>()
        //     .HasMany(s => s.HistoricalPrices)
        //     .WithOne(hp => hp.Stock)
        //     .HasForeignKey(hp => hp.StockId);

        // modelBuilder.Entity<Stock>()
        //     .HasMany(s => s.LivePrices)
        //     .WithOne(lp => lp.Stock)
        //     .HasForeignKey(lp => lp.StockId);

        // Configure decimal precision for price properties
        modelBuilder.Entity<HistoricalPrice>(entity =>
        {
            entity.Property(e => e.Open).HasColumnType("decimal(18,2)");
            entity.Property(e => e.High).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Low).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Close).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<LivePrice>(entity =>
        {
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
        });

        // Seed initial data if needed (example for Exchange enum, though not typical for DbContext seeding)
        // modelBuilder.Entity<StockTrader.Core.Enums.Exchange>().HasData( ... ); // This is not how enums are seeded.
                                                                            // Enums are part of the model, not data.
                                                                            // Seeding actual Stock entities could be done here.
        // Example: Add an index
        // modelBuilder.Entity<Stock>().HasIndex(s => s.Symbol).IsUnique();
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasIndex(s => s.Symbol).IsUnique();
        });
         modelBuilder.Entity<SentimentData>(entity =>
        {
            entity.Property(e => e.SentimentScore).HasColumnType("decimal(3,2)"); // Precision for sentiment score
        });

        modelBuilder.Entity<TradingTip>(entity =>
        {
            entity.Property(e => e.ConfidenceScore).HasColumnType("decimal(3,2)"); // Precision for confidence score
        });
    }
}
